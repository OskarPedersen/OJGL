#pragma once

#include "demo/Demo.h"
#include "render/Window.h"
#include "utility/Timepoint.h"
#include "utility/Vector.h"

namespace ojgl {

struct PlayerState {
    Vector3f positionM { 10.0f, 10.5f, 0.0f };
    Vector3f velocityMS { 0.0f, 0.0f, 0.0f };
    float headingRad = 0.0f;
};

struct GameState {
    PlayerState player;

    static GameState& instance();
    void update(const Window& window);

private:
    static constexpr float _thrustAccelMS2 = 10.5f;
    static constexpr float _turnSpeedRadS = 3.0f;
    static constexpr float _gravityMS2 = 9.81f;
    static constexpr float _groundLevelM = 0.0f;

    
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
