#include "GLState.h"
#include "Uniform.hpp"
#include "music/Music.h"
#include "utility\Timer.hpp"
#include <iostream>
#include <memory>
#include <string>
#include <vector>

namespace ojgl {

GLState::GLState()
{
    load_gl_functions();
    setupQuad();
    _startTime = timer::clock_t::now();
    _paused = false;
}

GLState::~GLState()
{
    glDeleteVertexArrays(1, &_vaoID);
    glDeleteBuffers(1, &_vboID);
}

void GLState::setStartTime(timer::time_point_t time)
{
    _startTime = time;
}

void GLState::addScene(const Scene& scene)
{
    _scenes.push_back(scene);
}

bool GLState::render()
{
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    glBindVertexArray(_vaoID);

    auto t = timer::ms_t(0);
    auto elapsed = elapsedTime();
    bool end = true;
    for (auto& v : _scenes) {
        if (elapsed < v.duration() + t) {
            v.render();
            end = false;
            break;
        }
        t = t + v.duration();
    }

    glBindVertexArray(0);
    glFlush();
    glFinish();
    return end;
}

Scene& GLState::operator[](size_t i)
{
    return _scenes[i];
}

void GLState::setupQuad()
{
    float vertices[] = {
        -1, 1, 0, -1, -1, 0, 1, -1, 0,
        1, -1, 0, 1, 1, 0, -1, 1, 0
    };

    glGenVertexArrays(1, &_vaoID);
    glBindVertexArray(_vaoID);
    glEnableVertexAttribArray(0);
    glGenBuffers(1, &_vboID);
    glBindBuffer(GL_ARRAY_BUFFER, _vboID);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), &vertices, GL_STATIC_DRAW);
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 0, nullptr);
    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindVertexArray(0);
}

timer::ms_t GLState::elapsedTime() const
{
    auto elapsed = timer::clock_t::now() - _startTime;
    if (_paused) {
        elapsed = _pauseTime - _startTime;
    }
    return timer::duration_cast<timer::ms_t>(elapsed);
}

bool GLState::isPaused()
{
    return _paused;
}

void GLState::clearScenes()
{
    _scenes.clear();
}

GLuint GLState::getVAO() const
{
    return this->_vaoID;
}

GLuint GLState::getVBO() const
{
    return this->_vboID;
}

timer::time_point_t GLState::startTime() const
{
    return this->_startTime;
}

void GLState::togglePause()
{
    if (_paused) {
        _startTime += timer::clock_t::now() - _pauseTime;
    }
    _paused = !_paused;
    _pauseTime = timer::clock_t::now();
}

timer::ms_t GLState::relativeSceneTime() const
{
    auto t = timer::ms_t(0);
    auto elapsed = elapsedTime();
    for (auto& v : _scenes) {
        if (elapsed < v.duration() + t) {
            break;
        }
        t = t + v.duration();
    }
    return elapsed - t;
}

void GLState::restart()
{
    _startTime = timer::clock_t::now();
    _pauseTime = _startTime;
}

void GLState::nextScene()
{
    auto t = timer::ms_t(0);
    auto elapsed = elapsedTime();
    for (auto& v : _scenes) {
        if (elapsed < v.duration() + t) {
            changeTime(v.duration() - relativeSceneTime());
            break;
        }
        t = t + v.duration();
    }
}

void GLState::previousScene()
{
    auto t = timer::ms_t(0);
    auto prevDur = timer::ms_t(0);
    auto elapsed = elapsedTime();
    for (auto& v : _scenes) {
        if (elapsed < v.duration() + t) {
            changeTime(-relativeSceneTime() - prevDur);
            break;
        }
        t = t + v.duration();
        prevDur = v.duration();
    }
}
} //namespace ojgl