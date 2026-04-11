#pragma once

#include "demo/Demo.h"
#include "render/Window.h"
#include "utility/Timepoint.h"
#include "utility/Vector.h"

namespace ojgl {

struct GameState {
    Vector3f playerPosition { 0.0f, 0.0f, 0.0f };

    static GameState& instance();
    void update(const Window& window);

private:
    static constexpr float _moveSpeed = 0.01f;
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
