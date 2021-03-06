#include "Mesh.h"
#include "utility/OJstd.h"
#include "winapi/gl_loader.h"

using namespace ojgl;

struct BufferIndices {
    unsigned int vaoID = 0;
    unsigned int vboID = 0;
    unsigned int nboID = 0;
};

static BufferIndices generateVAO(const ojstd::vector<float>& vertices, const ojstd::vector<float>& normals = {})
{
    unsigned int vaoID, vboID, nboID;

    glGenVertexArrays(1, &vaoID);
    glBindVertexArray(vaoID);

    if (!vertices.empty()) {
        glEnableVertexAttribArray(0);
        glGenBuffers(1, &vboID);
        glBindBuffer(GL_ARRAY_BUFFER, vboID);
        glBufferData(GL_ARRAY_BUFFER, vertices.size() * sizeof(float), vertices.begin(), GL_STATIC_DRAW);
        glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 0, nullptr);
    }

    if (!normals.empty()) {
        glGenBuffers(1, &nboID);
        glBindBuffer(GL_ARRAY_BUFFER, nboID);
        glEnableVertexAttribArray(1);
        glBufferData(GL_ARRAY_BUFFER, normals.size() * sizeof(float), normals.begin(), GL_STATIC_DRAW);
        glVertexAttribPointer(1, 3, GL_FLOAT, false, 0, nullptr);
    }

    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindVertexArray(0);

    return { vaoID, vboID, nboID };
}

Mesh::Mesh(const ojstd::vector<float>& vertices, const ojstd::vector<float>& normals)
{
    auto[vaoID, vboID, nboID] = generateVAO(vertices, normals);
    _vaoID = vaoID;
    _vboID = vboID;
    _nboID = nboID;
    _verticesCount = vertices.size();
}

Mesh::~Mesh()
{
    if (_vaoID)
        glDeleteVertexArrays(1, &_vaoID);
    if (_vboID)
        glDeleteBuffers(1, &_vboID);
    if (_nboID)
        glDeleteBuffers(1, &_nboID);
}

ojstd::shared_ptr<Mesh> Mesh::construct(const ojstd::vector<float>& vertices)
{
    return new Mesh(vertices);
}

ojstd::shared_ptr<Mesh> Mesh::constructQuad()
{
    ojstd::vector<float> vertices = {
        -1, 1, 0,
        -1, -1, 0,
        1, -1, 0,
        1, -1, 0,
        1, 1, 0,
        -1, 1, 0
    };
    ojstd::vector<float> normals = {
        0, 0, 1,
        0, 0, 1,
        0, 0, 1,
        0, 0, 1,
        0, 0, 1,
        0, 0, 1,
    };
    return new Mesh(vertices, normals);
}

ojstd::shared_ptr<Mesh> Mesh::constructCube()
{
    // clang-format off
    ojstd::vector<float> vertices = {
        // Front face
        -1.0, -1.0, 1.0,
         1.0, -1.0, 1.0,
         1.0,  1.0, 1.0,

        -1.0,  1.0, 1.0,
         1.0,  1.0, 1.0,
        -1.0, -1.0, 1.0,

        // Back face
         1.0, -1.0, -1.0,
        -1.0, -1.0, -1.0,
         1.0,  1.0, -1.0,

        -1.0,  1.0, -1.0,
         1.0,  1.0, -1.0,
        -1.0, -1.0, -1.0,

        // Top face
        -1.0, 1.0, -1.0, 
         1.0, 1.0, -1.0, 
         1.0, 1.0,  1.0, 

        -1.0, 1.0,  1.0, 
         1.0, 1.0,  1.0, 
        -1.0, 1.0, -1.0, 

        // Bottom face
        -1.0, -1.0, -1.0, 
         1.0, -1.0, -1.0, 
         1.0, -1.0,  1.0, 

        -1.0, -1.0,  1.0, 
         1.0, -1.0,  1.0, 
        -1.0, -1.0, -1.0,

        // Right face
        1.0, -1.0, -1.0, 
        1.0,  1.0, -1.0, 
        1.0,  1.0,  1.0, 

        1.0, -1.0,  1.0, 
        1.0,  1.0,  1.0, 
        1.0, -1.0, -1.0, 

        // Left face
        -1.0, -1.0, -1.0, 
        -1.0,  1.0, -1.0, 
        -1.0,  1.0,  1.0, 
        
        -1.0, -1.0,  1.0, 
        -1.0,  1.0,  1.0, 
        -1.0, -1.0, -1.0, 
    };
    // clang-format on

    ojstd::vector<float> normal = {
        // Front face
        0.0, 0.0, 1.0,
        0.0, 0.0, 1.0,
        0.0, 0.0, 1.0,
        0.0, 0.0, 1.0,
        0.0, 0.0, 1.0,
        0.0, 0.0, 1.0,

        // Back face
        0.0, 0.0, -1.0,
        0.0, 0.0, -1.0,
        0.0, 0.0, -1.0,
        0.0, 0.0, -1.0,
        0.0, 0.0, -1.0,
        0.0, 0.0, -1.0,

        // Top face
        0.0, 1.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 1.0, 0.0,

        // Bottom face
        0.0, -1.0, 0.0,
        0.0, -1.0, 0.0,
        0.0, -1.0, 0.0,
        0.0, -1.0, 0.0,
        0.0, -1.0, 0.0,
        0.0, -1.0, 0.0,

        // Right face
        1.0, 0.0, 0.0,
        1.0, 0.0, 0.0,
        1.0, 0.0, 0.0,
        1.0, 0.0, 0.0,
        1.0, 0.0, 0.0,
        1.0, 0.0, 0.0,

        // Left face
        -1.0, 0.0, 0.0,
        -1.0, 0.0, 0.0,
        -1.0, 0.0, 0.0,
        -1.0, 0.0, 0.0,
        -1.0, 0.0, 0.0,
        -1.0, 0.0, 0.0
    };
    return new Mesh(vertices, normal);
}
