#pragma once

#include "demo/Demo.h"
#include "render/Window.h"
#include "utility/Timepoint.h"
#include "utility/Vector.h"

namespace ojgl {

struct GameState {
    Vector3f playerPosition { 0.0f, 0.5f, 0.0f };
    Vector3f velocity { 0.0f, 0.0f, 0.0f };
    float heading = 0.0f;

    static GameState& instance();
    void update(const Window& window);

private:
    static constexpr float _thrustAccel = 0.0015f;
    static constexpr float _turnSpeed = 0.003f;
    static constexpr float _elevatorStrength = 0.008f;
    static constexpr float _gravity = 0.012f;
    static constexpr float _groundLevel = 0.0f;
    static constexpr float _hoverHeight = 0.3f;
    static constexpr float _hoverStiffness = 0.03f;
    static constexpr float _hoverDamping = 0.06f;
    static constexpr float _dragHorizontal = 0.002f;
    static constexpr float _dragVertical = 0.004f;
    Timepoint _previousUpdateTime;
    bool _initialized = false;
};

class FzxGame final : public Demo {
public:
    ojstd::vector<Scene> buildSceneGraph(const Vector2i& sceneSize) const override;
    ojstd::string getTitle() const override;
    void update(const Duration& relativeSceneTime, const Duration& elapsedTime, const ojstd::string& currentScene) const override;
};

}
