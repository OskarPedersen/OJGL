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

    // Turning
    if (downKeys.contains(Window::KEY_LEFT)) {
        heading -= _turnSpeed * dt;
    }
    if (downKeys.contains(Window::KEY_RIGHT)) {
        heading += _turnSpeed * dt;
    }

    // Thrust along heading direction
    if (downKeys.contains(Window::KEY_SPACE)) {
        velocity.x -= ojstd::sin(heading) * _thrustAccel * dt;
        velocity.z -= ojstd::cos(heading) * _thrustAccel * dt;
    }

    // Elevator: up/down arrows give vertical impulse
    if (downKeys.contains(Window::KEY_UP)) {
        velocity.y += _elevatorStrength * dt;
    }
    if (downKeys.contains(Window::KEY_DOWN)) {
        velocity.y -= _elevatorStrength * dt;
    }

    // Gravity
    velocity.y -= _gravity * dt;

    // Hover force: spring pushes up when close to ground
    float distAboveGround = playerPosition.y - _groundLevel;
    if (distAboveGround < _hoverHeight) {
        float penetration = _hoverHeight - distAboveGround;
        velocity.y += penetration * _hoverStiffness * dt;
        velocity.y *= (1.0f - ojstd::min(_hoverDamping * dt, 1.0f));
    }

    // Hard floor
    if (playerPosition.y < _groundLevel) {
        playerPosition.y = _groundLevel;
        if (velocity.y < 0.0f)
            velocity.y = 0.0f;
    }

    // Drag
    float hDrag = 1.0f - ojstd::min(_dragHorizontal * dt, 1.0f);
    float vDrag = 1.0f - ojstd::min(_dragVertical * dt, 1.0f);
    velocity.x *= hDrag;
    velocity.z *= hDrag;
    velocity.y *= vDrag;

    // Integrate position
    playerPosition.x += velocity.x * dt;
    playerPosition.y += velocity.y * dt;
    playerPosition.z += velocity.z * dt;
}

ojstd::vector<Scene> FzxGame::buildSceneGraph(const Vector2i& sceneSize) const
{
    ojstd::vector<Scene> scenes;
    {
        auto game = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "fzxgame/game.fs");
        game->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            const auto& state = GameState::instance();
            vector.push_back(ojstd::make_shared<Uniform3fv>("iPlayerPosition", ojstd::vector<float>({ state.playerPosition.x, state.playerPosition.y, state.playerPosition.z })));
            vector.push_back(ojstd::make_shared<Uniform1f>("iPlayerHeading", state.heading));
            float speed = ojstd::sqrt(static_cast<float>(state.velocity.x * state.velocity.x + state.velocity.z * state.velocity.z));
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
