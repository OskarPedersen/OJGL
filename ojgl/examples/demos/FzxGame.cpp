#include "FzxGame.h"
#include "FreeCameraController.h"

namespace ojgl {

static GameState gameState;

GameState& GameState::instance()
{
    return gameState;
}

void GameState::update(const Window& window)
{
    if (!_initialized) {
        _previousUpdateTime = Timepoint::now();
        _initialized = true;
    }

    float dt = (Timepoint::now() - _previousUpdateTime).toMilliseconds();
    _previousUpdateTime = Timepoint::now();

    auto downKeys = window.getDownKeys();

    if (downKeys.contains(Window::KEY_W)) {
        playerPosition.z += _moveSpeed * dt;
    }
    if (downKeys.contains(Window::KEY_S)) {
        playerPosition.z -= _moveSpeed * dt;
    }
    if (downKeys.contains(Window::KEY_A)) {
        playerPosition.x -= _moveSpeed * dt;
    }
    if (downKeys.contains(Window::KEY_D)) {
        playerPosition.x += _moveSpeed * dt;
    }
    if (downKeys.contains(Window::KEY_Z)) {
        playerPosition.y += _moveSpeed * dt;
    }
    if (downKeys.contains(Window::KEY_X)) {
        playerPosition.y -= _moveSpeed * dt;
    }
}

ojstd::vector<Scene> FzxGame::buildSceneGraph(const Vector2i& sceneSize) const
{
    ojstd::vector<Scene> scenes;
    {
        auto game = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "fzxgame/game.fs");
        game->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            const auto& pos = GameState::instance().playerPosition;
            vector.push_back(ojstd::make_shared<Uniform3fv>("iPlayerPosition", ojstd::vector<float>({ pos.x, pos.y, pos.z })));
            return vector;
        });

        scenes.emplace_back(game, Duration::seconds(9999), "gameScene");
    }

    return scenes;
}

ojstd::string FzxGame::getTitle() const
{
    return "FZX Game";
}

void FzxGame::update(const Duration& relativeSceneTime, const Duration& elapsedTime, const ojstd::string& currentScene) const
{
    OJ_UNUSED(relativeSceneTime);
    OJ_UNUSED(elapsedTime);
    OJ_UNUSED(currentScene);
}

}
