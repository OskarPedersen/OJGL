#pragma once

#include "Scene.h"
#include "utility/Timer.hpp"
#include "winapi/gl_loader.h"
#include <functional>
#include <string>
#include <vector>

namespace ojgl {

class GLState {
public:
    GLState();
    ~GLState();

    bool render();
    void addScene(const Scene& scene);
    void setStartTime(timer::time_point_t time);
    void restart();
    void nextScene();
    void previousScene();
    void togglePause();
    bool isPaused();
    void clearScenes();

    timer::time_point_t startTime() const;
    timer::ms_t relativeSceneTime() const;
    timer::ms_t elapsedTime() const;

    Scene& operator[](size_t i);

private:
    void setupQuad();
    GLuint getVAO() const;
    GLuint getVBO() const;

    std::vector<Scene> _scenes;
    timer::time_point_t _startTime;
    timer::time_point_t _pauseTime;
    GLuint _vaoID;
    GLuint _vboID;
    bool _paused;

public:
    template <typename T>
    void changeTime(timer::duration_t<T> time);
};

template <typename T>
inline void GLState::changeTime(timer::duration_t<T> time)
{
    _startTime -= time;
}

} // namespace ojgl