#pragma once

#include "Scene.h"
#include "Uniform.hpp"
#include "music/Music.h"
#include "winapi/gl_loader.h"
#include <functional>
#include <set>
#include <string>
#include <unordered_set>
#include <vector>

namespace ojgl {

class GLState {
public:
    GLState(const std::string& vertexShader, const std::string& fragmentShader);
    ~GLState();
    void render();
    void addScene(const Scene& scene);

    GLuint getVAO() const;
    GLuint getVBO() const;
    DWORD startTime() const;

    template <typename T>
    friend GLState& operator<<(GLState& o, T& b);

private:
    void setupQuad();

    const unsigned _vertexCount = 6;
    DWORD _startTime;
    GLuint _vaoID;
    GLuint _vboID;
    std::map<std::string, std::shared_ptr<UniformBase>> _uniforms;
    std::vector<Scene> _scenes;
};

template <typename T>
GLState& operator<<(GLState& o, T& b)
{
    o._uniforms[b.location()] = std::make_shared<typename std::remove_reference<T>::type>(std::forward<T>(b));
    return o;
}

} // namespace ojgl