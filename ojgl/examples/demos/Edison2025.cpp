#include "Edison2025.h"
#include "FreeCameraController.h"

namespace ojgl {

Edison2025::Edison2025()
{
}

static const unsigned char song[] = {
#include "songs/edison_2025_song.inc"
};

const unsigned char* Edison2025::getSong() const
{
    return song;
}

ojstd::vector<Scene> Edison2025::buildSceneGraph(const Vector2i& sceneSize) const
{
    ojstd::vector<Scene> scenes;
    auto noise = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/noise.fs");
    noise->setRenderOnce(true);

    auto experiment = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/experiment.fs");
    experiment->setInputs(noise);

    experiment->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
        Buffer::UniformVector vector;
        vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
        return vector;
    });
    scenes.emplace_back(experiment, Duration::seconds(1000000), "experiment");
    return scenes;
}

ojstd::string Edison2025::getTitle() const
{
    return "Edison 2025";
}

void Edison2025::update(const Duration& relativeSceneTime, const Duration& elapsedTime, const ojstd::string& currentScene) const
{
    OJ_UNUSED(relativeSceneTime);
    OJ_UNUSED(elapsedTime);
    OJ_UNUSED(currentScene);
}

}
