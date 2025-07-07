#include "Edison2025.h"
#include "FreeCameraController.h"
#include "music/Music.h"

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
        
        auto music = Music::instance();

        vector.push_back(ojstd::make_shared<Uniform1f>("C_1_S", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));
        vector.push_back(ojstd::make_shared<Uniform1f>("C_6_S", music->syncChannels()[6].getTimeSinceAnyNote().toSeconds()));
        vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S", music->syncChannels()[7].getTimeSinceAnyNote().toSeconds()));
        
        vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_0", music->syncChannels()[7].getTimeSinceLast(0).toSeconds()));
        vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_1", music->syncChannels()[7].getTimeSinceLast(1).toSeconds()));
        vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_2", music->syncChannels()[7].getTimeSinceLast(2).toSeconds()));
        vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_3", music->syncChannels()[7].getTimeSinceLast(3).toSeconds()));

        vector.push_back(ojstd::make_shared<Uniform1f>("C_7_T", static_cast<float>(music->syncChannels()[7].getTotalHits())));

        return vector;
    });

    auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/blur1.fs");
    blur1->setInputs(experiment);
    blur1->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
        return { ojstd::make_shared<Uniform2f>("blurDir", 1.f, 0.f) };
    });

    auto blur2 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/blur1.fs");
    blur2->setInputs(blur1);
    blur2->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
        return { ojstd::make_shared<Uniform2f>("blurDir", 0.f, 1.f) };
    });

    auto chrom = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/chrom_ab.fs");
    chrom->setInputs(blur2);
    chrom->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
        Buffer::UniformVector vector;
        vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

        auto music = Music::instance();

        vector.push_back(ojstd::make_shared<Uniform1f>("C_1_S", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));

        return vector;
    });

    scenes.emplace_back(chrom, Duration::seconds(1000000), "experiment");
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
