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

    float dtS = (Timepoint::now() - _previousUpdateTime).toSeconds();
    _previousUpdateTime = Timepoint::now();


    auto downKeys = window.getDownKeys();

    // Reset
    if (downKeys.contains(Window::KEY_R)) {
        player = PlayerState{};
        return;
    }

    // Turning
    if (downKeys.contains(Window::KEY_LEFT)) {
        player.headingRad -= _turnSpeedRadS * dtS;
    }
    if (downKeys.contains(Window::KEY_RIGHT)) {
        player.headingRad += _turnSpeedRadS * dtS;
    }

    // Thrust along heading direction
    if (downKeys.contains(Window::KEY_SPACE)) {
        player.velocityMS.x -= ojstd::sin(player.headingRad) * _thrustAccelMS2 * dtS;
        player.velocityMS.z -= ojstd::cos(player.headingRad) * _thrustAccelMS2 * dtS;
    }

    

    // Gravity
    player.velocityMS.y -= _gravityMS2 * dtS;

    

    // Hard floor
    if (player.positionM.y < _groundLevelM) {
        player.positionM.y = _groundLevelM;
        if (player.velocityMS.y < 0.0f)
            player.velocityMS.y = 0.0f;
    }

    // Integrate position
    player.positionM.x += player.velocityMS.x * dtS;
    player.positionM.y += player.velocityMS.y * dtS;
    player.positionM.z += player.velocityMS.z * dtS;
}

ojstd::vector<Scene> FzxGame::buildSceneGraph(const Vector2i& sceneSize) const
{
    ojstd::vector<Scene> scenes;
    {
        auto game = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "fzxgame/game.fs");
        game->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            const auto& p = GameState::instance().player;
            vector.push_back(ojstd::make_shared<Uniform3fv>("iPlayerPosition", ojstd::vector<float>({ p.positionM.x, p.positionM.y, p.positionM.z })));
            vector.push_back(ojstd::make_shared<Uniform1f>("iPlayerHeading", p.headingRad));
            float speed = ojstd::sqrt(static_cast<float>(p.velocityMS.x * p.velocityMS.x + p.velocityMS.z * p.velocityMS.z));
            vector.push_back(ojstd::make_shared<Uniform1f>("iPlayerSpeed", speed));
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
