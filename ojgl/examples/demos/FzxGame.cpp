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

    // Reset
    if (downKeys.contains(Window::KEY_R)) {
        player = PlayerState{};
        return;
    }

    // Turning
    if (downKeys.contains(Window::KEY_LEFT)) {
        player.heading -= _turnSpeed * dt;
    }
    if (downKeys.contains(Window::KEY_RIGHT)) {
        player.heading += _turnSpeed * dt;
    }

    // Thrust along heading direction
    if (downKeys.contains(Window::KEY_SPACE)) {
        player.velocity.x -= ojstd::sin(player.heading) * _thrustAccel * dt;
        player.velocity.z -= ojstd::cos(player.heading) * _thrustAccel * dt;
    }

    // Elevator: up/down arrows give vertical impulse
    if (downKeys.contains(Window::KEY_UP)) {
        player.velocity.y += _elevatorStrength * dt;
    }
    if (downKeys.contains(Window::KEY_DOWN)) {
        player.velocity.y -= _elevatorStrength * dt;
    }

    // Gravity
    player.velocity.y -= _gravity * dt;

    // Hover force: spring pushes up when close to ground
    float distAboveGround = player.playerPosition.y - _groundLevel;
    if (distAboveGround < _hoverHeight) {
        float penetration = _hoverHeight - distAboveGround;
        player.velocity.y += penetration * _hoverStiffness * dt;
        player.velocity.y *= (1.0f - ojstd::min(_hoverDamping * dt, 1.0f));
    }

    // Hard floor
    if (player.playerPosition.y < _groundLevel) {
        player.playerPosition.y = _groundLevel;
        if (player.velocity.y < 0.0f)
            player.velocity.y = 0.0f;
    }

    // Drag
    float hDrag = 1.0f - ojstd::min(_dragHorizontal * dt, 1.0f);
    float vDrag = 1.0f - ojstd::min(_dragVertical * dt, 1.0f);
    player.velocity.x *= hDrag;
    player.velocity.z *= hDrag;
    player.velocity.y *= vDrag;

    // Integrate position
    player.playerPosition.x += player.velocity.x * dt;
    player.playerPosition.y += player.velocity.y * dt;
    player.playerPosition.z += player.velocity.z * dt;
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
            vector.push_back(ojstd::make_shared<Uniform3fv>("iPlayerPosition", ojstd::vector<float>({ p.playerPosition.x, p.playerPosition.y, p.playerPosition.z })));
            vector.push_back(ojstd::make_shared<Uniform1f>("iPlayerHeading", p.heading));
            float speed = ojstd::sqrt(static_cast<float>(p.velocity.x * p.velocity.x + p.velocity.z * p.velocity.z));
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
