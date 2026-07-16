#include "FreeCameraController.h"
#include "demo/Demo.h"
#include "music/Music.h"

namespace ojgl {
class Edison2026 final : public Demo {
public:
    ojstd::vector<Scene> buildSceneGraph(const Vector2i& sceneSize) const override;
    ojstd::string getTitle() const override;
};
}

using namespace ojgl;

ojstd::vector<Scene> Edison2026::buildSceneGraph(const Vector2i& sceneSize) const
{
    ojstd::vector<Scene> scenes;
    //{
    //    auto cube = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2026/cube.fs");
    //    cube->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
    //        Buffer::UniformVector vector;
    //        vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
    //        auto music = Music::instance();
    //        if (music != nullptr) {
    //            vector.push_back(ojstd::make_shared<Uniform1f>("mBassdrum", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));
    //            vector.push_back(ojstd::make_shared<Uniform1f>("mBassdrumTot", (float)music->syncChannels()[1].getTotalHits()));
    //            vector.push_back(ojstd::make_shared<Uniform1f>("mHihat", music->syncChannels()[2].getTimeSinceAnyNote().toSeconds()));
    //            vector.push_back(ojstd::make_shared<Uniform1f>("mHihatTot", (float)music->syncChannels()[2].getTotalHits()));
    //            vector.push_back(ojstd::make_shared<Uniform1f>("mSnare", music->syncChannels()[3].getTimeSinceAnyNote().toSeconds()));
    //            vector.push_back(ojstd::make_shared<Uniform1f>("mSnareTot", (float)music->syncChannels()[3].getTotalHits()));
    //            // introstrings is spread across channels 4-7 (one per chord voice); take the soonest hit and sum the hit counts.
    //            float stringsSince = ojstd::min(
    //                ojstd::min(music->syncChannels()[4].getTimeSinceAnyNote().toSeconds(), music->syncChannels()[5].getTimeSinceAnyNote().toSeconds()),
    //                ojstd::min(music->syncChannels()[6].getTimeSinceAnyNote().toSeconds(), music->syncChannels()[7].getTimeSinceAnyNote().toSeconds()));
    //            vector.push_back(ojstd::make_shared<Uniform1f>("mStrings", stringsSince));
    //            float stringsTot = (float)(music->syncChannels()[4].getTotalHits() + music->syncChannels()[5].getTotalHits()
    //                + music->syncChannels()[6].getTotalHits() + music->syncChannels()[7].getTotalHits());
    //            vector.push_back(ojstd::make_shared<Uniform1f>("mStringsTot", stringsTot));
    //        }
    //        return vector;
    //    });

    //    auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2026/blur1.fs");
    //    blur1->setInputs(cube);
    //    blur1->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
    //        return { ojstd::make_shared<Uniform2f>("blurDir", 1.f, 0.f) };
    //    });

    //    auto blur2 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2026/blur1.fs");
    //    blur2->setInputs(blur1);
    //    blur2->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
    //        return { ojstd::make_shared<Uniform2f>("blurDir", 0.f, 1.f) };
    //    });

    //    scenes.emplace_back(blur2, Duration::seconds(10), "cube");
    //}

    {
        auto intro = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2026/oskar_intro.fs");
        intro->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            auto music = Music::instance();
            if (music != nullptr) {
                vector.push_back(ojstd::make_shared<Uniform1f>("mBassdrum", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mBassdrumTot", (float)music->syncChannels()[1].getTotalHits()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mHihat", music->syncChannels()[2].getTimeSinceAnyNote().toSeconds()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mHihatTot", (float)music->syncChannels()[2].getTotalHits()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mSnare", music->syncChannels()[3].getTimeSinceAnyNote().toSeconds()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mSnareTot", (float)music->syncChannels()[3].getTotalHits()));
                // introstrings is spread across channels 4-7 (one per chord voice); take the soonest hit and sum the hit counts.
                float stringsSince = ojstd::min(
                    ojstd::min(music->syncChannels()[4].getTimeSinceAnyNote().toSeconds(), music->syncChannels()[5].getTimeSinceAnyNote().toSeconds()),
                    ojstd::min(music->syncChannels()[6].getTimeSinceAnyNote().toSeconds(), music->syncChannels()[7].getTimeSinceAnyNote().toSeconds()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mStrings", stringsSince));
                float stringsTot = (float)(music->syncChannels()[4].getTotalHits() + music->syncChannels()[5].getTotalHits()
                    + music->syncChannels()[6].getTotalHits() + music->syncChannels()[7].getTotalHits());
                vector.push_back(ojstd::make_shared<Uniform1f>("mStringsTot", stringsTot));
            }
            return vector;
        });

        auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2026/blur1.fs");
        blur1->setInputs(intro);
        blur1->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
            return { ojstd::make_shared<Uniform2f>("blurDir", 1.f, 0.f) };
        });

        auto blur2 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2026/blur1.fs");
        blur2->setInputs(blur1);
        blur2->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
            return { ojstd::make_shared<Uniform2f>("blurDir", 0.f, 1.f) };
        });

        scenes.emplace_back(blur2, Duration::seconds(20), "Intro");
    }

    {
        auto cube = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2026/oskar.fs");
        cube->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            auto music = Music::instance();
            if (music != nullptr) {
                vector.push_back(ojstd::make_shared<Uniform1f>("mBassdrum", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mBassdrumTot", (float)music->syncChannels()[1].getTotalHits()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mHihat", music->syncChannels()[2].getTimeSinceAnyNote().toSeconds()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mHihatTot", (float)music->syncChannels()[2].getTotalHits()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mSnare", music->syncChannels()[3].getTimeSinceAnyNote().toSeconds()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mSnareTot", (float)music->syncChannels()[3].getTotalHits()));
                // introstrings is spread across channels 4-7 (one per chord voice); take the soonest hit and sum the hit counts.
                float stringsSince = ojstd::min(
                    ojstd::min(music->syncChannels()[4].getTimeSinceAnyNote().toSeconds(), music->syncChannels()[5].getTimeSinceAnyNote().toSeconds()),
                    ojstd::min(music->syncChannels()[6].getTimeSinceAnyNote().toSeconds(), music->syncChannels()[7].getTimeSinceAnyNote().toSeconds()));
                vector.push_back(ojstd::make_shared<Uniform1f>("mStrings", stringsSince));
                float stringsTot = (float)(music->syncChannels()[4].getTotalHits() + music->syncChannels()[5].getTotalHits()
                    + music->syncChannels()[6].getTotalHits() + music->syncChannels()[7].getTotalHits());
                vector.push_back(ojstd::make_shared<Uniform1f>("mStringsTot", stringsTot));
            }
            return vector;
        });

        auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2026/blur1.fs");
        blur1->setInputs(cube);
        blur1->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
            return { ojstd::make_shared<Uniform2f>("blurDir", 1.f, 0.f) };
        });

        auto blur2 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2026/blur1.fs");
        blur2->setInputs(blur1);
        blur2->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
            return { ojstd::make_shared<Uniform2f>("blurDir", 0.f, 1.f) };
        });

        scenes.emplace_back(blur2, Duration::seconds(9999), "Oskar");
    }

    return scenes;
}

ojstd::string Edison2026::getTitle() const
{
    return "Edison 2026";
}

ojstd::shared_ptr<Demo> ojgl::createSelectedDemo()
{
    return ojstd::make_shared<Edison2026>();
}
