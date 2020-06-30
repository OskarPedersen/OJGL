#pragma once

#include "render/Window.h"
#include "utility/Matrix.h"
#include "utility/OJstd.h"
#include "utility/Vector.h"

namespace ojgl {

class FreeCameraController {

public:
    void update(const Window& window)
    {
        Vector2i cursorPosition = { window.getCursorPosition().x, window.getCursorPosition().y };
        auto downKeys = window.getDownKeys();

        if (window.isLeftMouseButtonDown()) {
            int dx = ojstd::sign(cursorPosition.x - this->_previousCursorPosition.x);
            int dy = ojstd::sign(cursorPosition.y - this->_previousCursorPosition.y);
            this->_heading -= this->_rotationSpeed * dx;
            this->_elevation -= this->_rotationSpeed * dy;
        }

        if (downKeys.contains(Window::KEY_W)) {
            this->_position.z -= this->_translationSpeed * ojstd::cos(this->_heading);
            this->_position.x -= this->_translationSpeed * ojstd::sin(this->_heading);
        }
        if (downKeys.contains(Window::KEY_S)) {
            this->_position.z += this->_translationSpeed * ojstd::cos(this->_heading);
            this->_position.x += this->_translationSpeed * ojstd::sin(this->_heading);
        }
        if (downKeys.contains(Window::KEY_D)) {
            this->_position.z += this->_translationSpeed * ojstd::cos(this->_heading + ojstd::pi / 2.f);
            this->_position.x += this->_translationSpeed * ojstd::sin(this->_heading + ojstd::pi / 2.f);
        }
        if (downKeys.contains(Window::KEY_A)) {
            this->_position.z -= this->_translationSpeed * ojstd::cos(this->_heading + ojstd::pi / 2.f);
            this->_position.x -= this->_translationSpeed * ojstd::sin(this->_heading + ojstd::pi / 2.f);
        }
        if (downKeys.contains(Window::KEY_Z))
            this->_position.y += this->_translationSpeed;
        if (downKeys.contains(Window::KEY_X))
            this->_position.y -= this->_translationSpeed;

        this->_previousCursorPosition = cursorPosition;
    }

    Matrix getCameraMatrix() const
    {
        float sh = ojstd::sin(this->_heading);
        float se = ojstd::sin(this->_elevation);
        float ch = ojstd::cos(this->_heading);
        float ce = ojstd::cos(this->_elevation);
        float px = this->_position.x;
        float py = this->_position.y;
        float pz = this->_position.z;
        float rotationMatrix[16] = { ch, 0, -sh, 0, se * sh, ce, ch * se, 0, sh * ce, -se, ch * ce, 0, px, py, pz, 1 };
        return Matrix(rotationMatrix);
    }

private:
    Vector3f _position{ 2, 4, 9 };
    float _heading = 0.f;
    float _elevation = 0.f;
    Vector2i _previousCursorPosition;

    static constexpr float _translationSpeed = 0.025f;
    static constexpr float _rotationSpeed = 0.02f;
};
}
