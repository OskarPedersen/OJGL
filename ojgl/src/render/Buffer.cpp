#include "Buffer.h"
#include "utility/Log.h"
#include "utility/ShaderReader.h"
#include "winapi/gl_loader.h"

using namespace ojgl;

Buffer::FBO::FBO(const Vector2i& size, int numOutBuffers, bool includeDepthBuffer, bool isOutputBuffer)
{
    // Just use fboID = 0 if this is an output buffer.
    if (isOutputBuffer)
        return;

    glGenFramebuffers(1, &_fboID);
    glBindFramebuffer(GL_FRAMEBUFFER, _fboID);

    if (includeDepthBuffer) {
        glGenTextures(1, &_depthID);
        glBindTexture(GL_TEXTURE_2D, _depthID);
        glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);

        glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH24_STENCIL8, size.x, size.y, 0, GL_DEPTH_STENCIL, GL_UNSIGNED_INT_24_8, NULL);
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_TEXTURE_2D, _depthID, 0);
    }

    for (int i = 0; i < numOutBuffers; i++) {
        unsigned fboTextureID = 0;
        glGenTextures(1, &fboTextureID);
        _fboTextureIDs.push_back(fboTextureID);
        glBindTexture(GL_TEXTURE_2D, _fboTextureIDs[i]);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, size.x, size.y, 0, GL_RGBA, GL_UNSIGNED_BYTE, nullptr);

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0 + i, GL_TEXTURE_2D, _fboTextureIDs[i], 0);

        if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE) {
            LOG_ERROR("Framebuffer error");
        }
    }

    ojstd::vector<GLenum> drawBuffers;
    for (int i = 0; i < numOutBuffers; i++) {
        drawBuffers.emplace_back(GL_COLOR_ATTACHMENT0 + i);
    }
    glDrawBuffers(numOutBuffers, &drawBuffers[0]);

    glBindTexture(GL_TEXTURE_2D, 0);
    glBindFramebuffer(GL_FRAMEBUFFER, 0);
}

Buffer::FBO::~FBO()
{
    if (_fboID != 0) {
        glDeleteFramebuffers(1, &_fboID);
    }

    for (int i = 0; i < _fboTextureIDs.size(); i++) {
        if (_fboTextureIDs[i] != 0) {
            glDeleteTextures(1, &_fboTextureIDs[i]);
        }
    }

    if (_depthID != 0) {
        glDeleteTextures(1, &_depthID);
    }
}

int Buffer::getNumberOfInputs(const ojstd::vector<BufferPtr>& inputs)
{
    int numInputs = 0;
    for (int i = 0; i < inputs.size(); i++) {
        numInputs += inputs[i]->numOutTextures();
    }
    return numInputs;
}

Buffer::Buffer(unsigned width, unsigned height, const ojstd::string& vertexPath, const ojstd::string& fragmentPath)
    : _width(width)
    , _height(height)
    , _vertexPath(vertexPath)
    , _fragmentPath(fragmentPath)
{
    setFormat(BufferFormat::Quad);
    loadShader();
}

Buffer::~Buffer()
{
    glDeleteProgram(_programID);
}

Buffer& Buffer::setFormat(BufferFormat format)
{
    _format = format;
    _meshes.clear();
    if (_format == BufferFormat::Quad)
        _meshes.push_back({ Mesh::constructQuad(), Matrix::identity() });
    return *this;
}

Buffer& Buffer::setDepthTest(bool depthTestEnabled)
{
    _depthTestEnabled = depthTestEnabled;
    return *this;
}

Buffer& Buffer::setRenderOnce(bool renderOnce)
{
    _renderOnce = renderOnce;
    return *this;
}

Buffer& Buffer::setNumOutTextures(int numOutTextures)
{
    _numOutTextures = numOutTextures;
    return *this;
}

Buffer& Buffer::setName(const ojstd::string& name)
{
    _name = name;
    return *this;
}

Buffer& Buffer::setViewportOffset(const Vector2i& viewportOffset)
{
    _viewportOffset = viewportOffset;
    return *this;
}

Buffer& Buffer::setUniformCallback(const ojstd::function<UniformVector(float)>& uniformCallback)
{
    _uniformCallback = uniformCallback;
    return *this;
}

Buffer& Buffer::setMeshCallback(const ojstd::function<ojstd::vector<ojstd::Pair<ojstd::shared_ptr<Mesh>, Matrix>>(float)>& meshCallback)
{
    _meshCallback = meshCallback;
    return *this;
}

Buffer& Buffer::setTextureCallback(const ojstd::function<ojstd::vector<ojstd::shared_ptr<Uniform1t>>(float)>& textureCallback)
{
    _textureCallback = textureCallback;
    return *this;
}

ojstd::string Buffer::name() const
{
    return _name;
}

#define GL_CHECK_ERROR(functionCall)          \
    functionCall;                             \
    {                                         \
        GLenum error = glGetError();          \
        if (error != GL_NO_ERROR) {           \
            _ASSERTE(false);                  \
            LOG_ERROR("GL Error: " << error); \
        }                                     \
    }

#define GL_CHECK_LOG(functionCall, id, logFunction, errorStr)                       \
    functionCall;                                                                   \
    if (param == GL_FALSE) {                                                        \
        LOG_ERROR(errorStr);                                                        \
        int len;                                                                    \
        constexpr int logSize = 200;                                                \
        char log[logSize];                                                          \
        logFunction(id, logSize, &len, log);                                        \
        _ASSERT_EXPR(len <= logSize, "Could not fit entire log, increase logSize"); \
        LOG_ERROR(log);                                                             \
    }

void Buffer::render(float relativeSceneTime, float absoluteTime)
{
    if (ShaderReader::modified(_vertexPath) || ShaderReader::modified(_fragmentPath)) {
        loadShader();
        _hasRendered = false;
    }

    if (_hasRendered && _renderOnce)
        return;

    {
        GLint validationStatus;
        glValidateProgram(_programID);
        glGetProgramiv(_programID, GL_VALIDATE_STATUS, &validationStatus);
        if (validationStatus == GL_FALSE) {
            // If e.g. a shader fails to compile, the program won't be valid.
            // So break early to avoid errors further down in this function.
            return;
        }
    }

    // Render to the next buffer.
    auto& currentFBO = pushNextFBO();
    GL_CHECK_ERROR(glBindFramebuffer(GL_FRAMEBUFFER, currentFBO.fboID()));
    GL_CHECK_ERROR(glViewport(_viewportOffset.x, _viewportOffset.y, _width, _height));
    GL_CHECK_ERROR(glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT));
    if (_depthTestEnabled) {
        GL_CHECK_ERROR(glEnable(GL_DEPTH_TEST));
    } else {
        GL_CHECK_ERROR(glDisable(GL_DEPTH_TEST));
    }

    GL_CHECK_ERROR(glUseProgram(_programID));

    int currentTextureID = 0;
    for (int i = 0, textureUniformID = 0; i < _inputs.size(); i++) {
        auto textureIDs = _inputs[i]->currentFBO().fboTextureIDs();
        for (int j = 0; j < textureIDs.size(); j++) {
            auto uniform = "inTexture" + ojstd::to_string(textureUniformID);
            GL_CHECK_ERROR(glUniform1i(glGetUniformLocation(_programID, uniform.c_str()), currentTextureID));
            GL_CHECK_ERROR(glActiveTexture(GL_TEXTURE0 + currentTextureID));
            GL_CHECK_ERROR(glBindTexture(GL_TEXTURE_2D, textureIDs[j]));
            currentTextureID++;
            textureUniformID++;
        }
    }

    if (_textureCallback) {
        // LOG_INFO("Has textures: " << _textureCallback(relativeSceneTime).size());
        for (const auto& texture : _textureCallback(relativeSceneTime)) {
            GL_CHECK_ERROR(glUniform1i(glGetUniformLocation(_programID, texture->location().c_str()), currentTextureID));
            GL_CHECK_ERROR(glActiveTexture(GL_TEXTURE0 + currentTextureID));
            GL_CHECK_ERROR(glBindTexture(GL_TEXTURE_2D, texture->textureID()));
            currentTextureID++;
        }
    }

    for (int i = 0, textureUniformID = 0; i < _feedbackInputs.size(); i++) {
        auto textureIDs = _feedbackInputs[i]->previousFBO().fboTextureIDs();
        for (int j = 0; j < textureIDs.size(); j++) {
            auto uniform = "feedbackTexture" + ojstd::to_string(textureUniformID);
            GL_CHECK_ERROR(glUniform1i(glGetUniformLocation(_programID, uniform.c_str()), currentTextureID));
            GL_CHECK_ERROR(glActiveTexture(GL_TEXTURE0 + currentTextureID));
            GL_CHECK_ERROR(glBindTexture(GL_TEXTURE_2D, textureIDs[j]));
            currentTextureID++;
            textureUniformID++;
        }
    }

    if (_meshCallback) {
        for (const auto& [meshPtr, matrix] : _meshCallback(relativeSceneTime)) {
            insertMesh(meshPtr, matrix);
        }
    }

    if (_uniformCallback) {
        for (const auto& uniform : _uniformCallback(relativeSceneTime)) {
            uniform->setUniform(_programID);
        }
    }

    GL_CHECK_ERROR(glUniform1f(glGetUniformLocation(_programID, "iTime"), relativeSceneTime));
    GL_CHECK_ERROR(glUniform1f(glGetUniformLocation(_programID, "iAbsoluteTime"), absoluteTime));
    GL_CHECK_ERROR(glUniform2f(glGetUniformLocation(_programID, "iResolution"), static_cast<GLfloat>(_width), static_cast<GLfloat>(_height)));

    for (const auto& mesh : _meshes) {
        GL_CHECK_ERROR(glBindVertexArray(mesh.first->vaoID()));
        GL_CHECK_ERROR(glUniformMatrix4fv(glGetUniformLocation(_programID, "M"), 1, false, mesh.second.data()));
        if (mesh.first->usesIndices()) {
            GL_CHECK_ERROR(glDrawElements(GL_TRIANGLES, mesh.first->verticesCount(), GL_UNSIGNED_INT, nullptr));
        } else {
            GL_CHECK_ERROR(glDrawArrays(GL_TRIANGLES, 0, mesh.first->verticesCount()));
        }
    }

    GL_CHECK_ERROR(glBindVertexArray(0));

    GL_CHECK_ERROR(glUseProgram(0));
    GL_CHECK_ERROR(glBindFramebuffer(GL_FRAMEBUFFER, 0));

    _hasRendered = true;
}

const Buffer::FBO& Buffer::pushNextFBO()
{
    _currentFBOIndex = (_currentFBOIndex + 1) % 2;
    return _fbos[_currentFBOIndex];
}

const Buffer::FBO& Buffer::currentFBO() const
{
    return _fbos[_currentFBOIndex];
}

const Buffer::FBO& Buffer::previousFBO() const
{
    return _fbos[(_currentFBOIndex + 1) % 2];
}

void Buffer::generateFBO(bool isOutputBuffer)
{
    // Generate two fbo's. Each fbo will be rendered to every other frame.
    for (int i = 0; i < 2; i++)
        _fbos.emplace_back(Vector2i(_width, _height), _numOutTextures, _format == BufferFormat::Meshes, isOutputBuffer);
}

void Buffer::loadShader()
{
    _ASSERTE(glGetError() == GL_NO_ERROR);
    if (glIsProgram(_programID)) {
        GL_CHECK_ERROR(glDeleteProgram(_programID));
    }

    _programID = glCreateProgram();
    _ASSERTE(_programID != 0);

    int vertID = glCreateShader(GL_VERTEX_SHADER);
    int fragID = glCreateShader(GL_FRAGMENT_SHADER);

    GLint param;

    // Vertex shader
    ojstd::string vertexShader = "#version 430\n" + ShaderReader::get(_vertexPath);
    int vertexShaderLength = vertexShader.length();
    auto vertexChar = vertexShader.c_str();
    GL_CHECK_ERROR(glShaderSource(vertID, 1, &vertexChar, &vertexShaderLength));
    GL_CHECK_ERROR(glCompileShader(vertID));
    GL_CHECK_LOG(glGetShaderiv(vertID, GL_COMPILE_STATUS, &param), vertID, glGetShaderInfoLog, "Failed to compile vertex shader!");

    // Fragment shader
    ojstd::string fragmentShader = "#version 430\n" + ShaderReader::get(_fragmentPath);
    int fragmentShaderLength = fragmentShader.length();
    auto fragmentChar = fragmentShader.c_str();
    GL_CHECK_ERROR(glShaderSource(fragID, 1, &fragmentChar, &fragmentShaderLength));
    GL_CHECK_ERROR(glCompileShader(fragID));
    GL_CHECK_LOG(glGetShaderiv(fragID, GL_COMPILE_STATUS, &param), fragID, glGetShaderInfoLog, "Failed to compile fragment shader!");

    // Attach shaders
    glAttachShader(_programID, vertID);
    glAttachShader(_programID, fragID);

    // Link program
    GL_CHECK_ERROR(glLinkProgram(_programID));
    GL_CHECK_LOG(glGetProgramiv(_programID, GL_LINK_STATUS, &param), _programID, glGetProgramInfoLog, "Shader program linking did not succeed.");

    // Validate program
    GL_CHECK_ERROR(glValidateProgram(_programID));
    GL_CHECK_LOG(glGetProgramiv(_programID, GL_VALIDATE_STATUS, &param), _programID, glGetProgramInfoLog, "Shader program is not valid!");

    // Delete the shaders
    GL_CHECK_ERROR(glDetachShader(_programID, vertID));
    GL_CHECK_ERROR(glDetachShader(_programID, fragID));
    GL_CHECK_ERROR(glDeleteShader(vertID));
    GL_CHECK_ERROR(glDeleteShader(fragID));
}

#undef GL_CHECK_ERROR
#undef GL_CHECK_LOG

int inline Buffer::numOutTextures()
{
    return _numOutTextures;
}

void Buffer::insertMesh(const ojstd::shared_ptr<Mesh>& mesh, const Matrix& modelMatrix)
{
    _ASSERTE(_format == BufferFormat::Meshes);
    _meshes.push_back({ mesh, modelMatrix });
}

void Buffer::clearMeshes()
{
    if (_format == BufferFormat::Meshes)
        _meshes.clear();
}

ojstd::shared_ptr<Buffer> Buffer::construct(unsigned width, unsigned height, const ojstd::string& vertexPath, const ojstd::string& fragmentPath)
{
    return ojstd::shared_ptr<Buffer>(new Buffer(width, height, vertexPath, fragmentPath));
}
