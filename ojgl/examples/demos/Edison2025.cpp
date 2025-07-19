#include "Edison2025.h"
#include "FreeCameraController.h"
#include "TextRenderer.hpp"
#include "music/Music.h"
#include "utility/Log.h"

namespace ojgl {

namespace {
    float previousTimes[] = { 0.0f, 0.0f };
    int previousTimeIndex = 0;

    // clang-format off
float xs[] = {0.0f, -0.9900000000000001f, -0.32010000000000016f, 0.9867990000000001f, -0.98013201f, -0.9279227199000006f, 0.9807207728010001f, -0.42920007652400916f, -0.99429200076524f, 0.9800570799923474f, };
float ys[] = {0.0f, -0.594f, -0.9959399999999999f, -0.21180059999999978f, 0.9683580060000004f, 0.99968358006f, -0.14550251556060056f, -0.9914550251556061f, -0.6523998706108676f, 0.532209577843685f, };
float ts[] = {0.0f, 1.16619037896906f, 1.9553125354047907f, 3.494800454681847f, 5.811786333427197f, 5.873287315050857f, 8.1216122423941f, 9.782456817738254f, 10.44811819681223f, 12.773842340959611f, };
constexpr int N = 10;
    // clang-format on
}

Edison2025::Edison2025()
{
}

ojstd::shared_ptr<Texture> Edison2025::getText(const ojstd::string& text, const ojstd::string& font) const
{
    if (!this->_textures.contains(text + font))
        this->_textures[text + font] = TextRenderer::instance().get(text, font);

    return this->_textures[text + font];
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

    // Borgila scene
    {
        auto noise = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/noise.fs");
        noise->setRenderOnce(true);

        auto stars = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/stars.fs");
        stars->setRenderOnce(true);

        auto experiment = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/borgila.fs");
        experiment->setInputs(noise, stars);

        experiment->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            const int synt = 3;
            auto music = Music::instance();
            vector.push_back(ojstd::make_shared<Uniform1f>("C_1_S", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_6_S", music->syncChannels()[6].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_4_S", music->syncChannels()[synt].getTimeSinceAnyNote().toSeconds()));

            vector.push_back(ojstd::make_shared<Uniform1f>("C_4_T", static_cast<float>(music->syncChannels()[synt].getTotalHits())));
            return vector;
        });

        experiment->setTextureCallback([this]([[maybe_unused]] float relativeSceneTime) {
            ojstd::vector<ojstd::shared_ptr<Uniform1t>> vector;
            vector.push_back(ojstd::make_shared<Uniform1t>("borgilaTexture", this->getText("BORGILA", "Arial Black")));
            return vector;
        });

        scenes.emplace_back(experiment, Duration::seconds(27), "borgila");
    }

    // indoor scene
    {
        auto noise = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/noise.fs");
        noise->setRenderOnce(true);

        auto stars = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/stars.fs");
        stars->setRenderOnce(true);

        // lissajous
        auto lissajous = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/lissajous.fs");
        lissajous->setFeedbackInputs(lissajous);

        lissajous->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<Uniform1f>("iPreviousTime", previousTimes[previousTimeIndex]));
            auto music = Music::instance();
            vector.push_back(ojstd::make_shared<Uniform1f>("C_1_S", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S", music->syncChannels()[7].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_1_T", static_cast<float>(music->syncChannels()[1].getTotalHits())));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_T", static_cast<float>(music->syncChannels()[0].getTotalHits())));

            return vector;
        });

        // radar
        auto radar = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/radar.fs");
        radar->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<Uniform1f>("iPreviousTime", previousTimes[previousTimeIndex]));
            return vector;
        });
        radar->setFeedbackInputs(radar);

        // oj text
        auto ojText = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/oj_text.fs");
        ojText->setFeedbackInputs(ojText);
        ojText->setTextureCallback([this]([[maybe_unused]] float relativeSceneTime) {
            ojstd::vector<ojstd::shared_ptr<Uniform1t>> vector;
            vector.push_back(ojstd::make_shared<Uniform1t>("ojTexture", this->getText("OJ", "Arial Black")));
            return vector;
        });

        ojText->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<Uniform1f>("iPreviousTime", previousTimes[previousTimeIndex]));

            float x = xs[N - 1];
            float y = ys[N - 1];
            float t = 0.7f * relativeSceneTime;
            for (int i = 0; i < N - 1; i++) {
                if (t >= ts[i] && t < ts[i + 1]) {
                    float f = (t - ts[i]) / (ts[i + 1] - ts[i]);
                    x = xs[i] + f * (xs[i + 1] - xs[i]);
                    y = ys[i] + f * (ys[i + 1] - ys[i]);
                    break;
                }
            }
            vector.push_back(ojstd::make_shared<Uniform1f>("x", x));
            vector.push_back(ojstd::make_shared<Uniform1f>("y", y));
            return vector;
        });

        auto experiment = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/indoor.fs");
        experiment->setInputs(noise, lissajous, radar, ojText, stars);

        experiment->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            return vector;
        });

        scenes.emplace_back(experiment, Duration::seconds(25), "indoor");
    }

    struct UfoScene {
        ojstd::string shader;
        Duration duration;
    };
    ojstd::vector<UfoScene> ufoScenes = {
        UfoScene {
            "edison2025/ufo_scene_1.fs",
            Duration::seconds(8) },
        UfoScene {
            "edison2025/ufo_scene_2.fs",
            Duration::seconds(12) },
        UfoScene {
            "edison2025/ufo_scene_3.fs",
            Duration::milliseconds(static_cast<long>(1000.0 * (10.0 + 6.5 + 10.0 + 4.0 - 2.0 - 12.0))) },
        UfoScene {
            "edison2025/ufo_scene_4.fs",
            Duration::milliseconds(static_cast<long>(1000.0 * (12.0))) }
    };

    // Ufo scenes

    for (const auto& ufoScene : ufoScenes) {
        auto noise = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/noise.fs");
        noise->setRenderOnce(true);
        auto stars = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/stars.fs");
        stars->setRenderOnce(true);

        auto ufoScenesScene = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", ufoScene.shader);
        ufoScenesScene->setInputs(noise, stars);

        ufoScenesScene->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();

            vector.push_back(ojstd::make_shared<Uniform1f>("C_1_S", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_5_S", music->syncChannels()[5].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_6_S", music->syncChannels()[6].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S", music->syncChannels()[7].getTimeSinceAnyNote().toSeconds()));

            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_0", music->syncChannels()[7].getTimeSinceLast(0).toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_1", music->syncChannels()[7].getTimeSinceLast(1).toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_2", music->syncChannels()[7].getTimeSinceLast(2).toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_3", music->syncChannels()[7].getTimeSinceLast(3).toSeconds()));

            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_T", static_cast<float>(music->syncChannels()[7].getTotalHits())));

            vector.push_back(ojstd::make_shared<Uniform1f>("C_1_T", static_cast<float>(music->syncChannels()[1].getTotalHits())));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_6_T", static_cast<float>(music->syncChannels()[6].getTotalHits())));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_3_T", static_cast<float>(music->syncChannels()[3].getTotalHits())));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_0_T", static_cast<float>(music->syncChannels()[0].getTotalHits())));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_2_T", static_cast<float>(music->syncChannels()[2].getTotalHits())));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_5_T", static_cast<float>(music->syncChannels()[5].getTotalHits())));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_4_T", static_cast<float>(music->syncChannels()[4].getTotalHits())));
            return vector;
        });

        ufoScenesScene->setTextureCallback([this]([[maybe_unused]] float relativeSceneTime) {
            ojstd::vector<ojstd::shared_ptr<Uniform1t>> vector;
            vector.push_back(ojstd::make_shared<Uniform1t>("borgilaTexture", this->getText("BORGILA", "Arial Black")));
            return vector;
        });

        auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/blur1.fs");
        blur1->setInputs(ufoScenesScene);
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

        scenes.emplace_back(chrom, ufoScene.duration, "ufo_scenes");
    }

    // Ufo hyperspace
    {
        auto noise = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/noise.fs");
        noise->setRenderOnce(true);

        auto ufoHyperSpace = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/ufo_hyperspace.fs");
        ufoHyperSpace->setInputs(noise);

        ufoHyperSpace->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
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

            // vector.push_back(ojstd::make_shared<Uniform1f>("C_1_S", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));
            // vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S", music->syncChannels()[7].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_1_T", static_cast<float>(music->syncChannels()[1].getTotalHits())));
            // vector.push_back(ojstd::make_shared<Uniform1f>("C_7_T", static_cast<float>(music->syncChannels()[0].getTotalHits())));

            return vector;
        });

        /* auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/blur1.fs");
         blur1->setInputs(ufoHyperSpace);
         blur1->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
             return { ojstd::make_shared<Uniform2f>("blurDir", 1.f, 0.f) };
         });

         auto blur2 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/blur1.fs");
         blur2->setInputs(blur1);
         blur2->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
             return { ojstd::make_shared<Uniform2f>("blurDir", 0.f, 1.f) };
         });*/

        // auto radialBlur = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "common/radial_blur.fs");
        // radialBlur->setInputs(ufoHyperSpace);

        auto chrom = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/chrom_ab_hyperspace.fs");
        chrom->setInputs(ufoHyperSpace);
        chrom->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();

            vector.push_back(ojstd::make_shared<Uniform1f>("C_1_S", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_1_T", static_cast<float>(music->syncChannels()[1].getTotalHits())));

            return vector;
        });

        scenes.emplace_back(chrom, Duration::seconds(9), "ufo_hyperspace");
    }

    // Ufo landing part 1
    {
        auto noise = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/noise.fs");
        noise->setRenderOnce(true);

        auto stars = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/stars.fs");
        stars->setRenderOnce(true);

        auto ufoLanding = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/ufo_landing.fs");
        ufoLanding->setInputs(noise, stars);

        ufoLanding->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
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

            vector.push_back(ojstd::make_shared<Uniform1f>("C_3_S", music->syncChannels()[3].getTimeSinceAnyNote().toSeconds()));

            vector.push_back(ojstd::make_shared<Uniform1f>("scenePart", 1.0f));

            return vector;
        });

        auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/blur1.fs");
        blur1->setInputs(ufoLanding);
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

        scenes.emplace_back(chrom, Duration::milliseconds(13500), "ufo_landing_part_1");
    }

    // Borgila hyperspace
    {
        auto noise = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/noise.fs");
        noise->setRenderOnce(true);

        auto borgilaHyperSpace = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/borgila_hyperspace.fs");
        borgilaHyperSpace->setInputs(noise);

        borgilaHyperSpace->setTextureCallback([this]([[maybe_unused]] float relativeSceneTime) {
            ojstd::vector<ojstd::shared_ptr<Uniform1t>> vector;
            vector.push_back(ojstd::make_shared<Uniform1t>("borgilaTexture", this->getText("BORGILA", "Arial Black")));
            return vector;
        });

        borgilaHyperSpace->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();

            vector.push_back(ojstd::make_shared<Uniform1f>("C_1_S", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_3_S", music->syncChannels()[3].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S", music->syncChannels()[7].getTimeSinceAnyNote().toSeconds()));

            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_0", music->syncChannels()[7].getTimeSinceLast(0).toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_1", music->syncChannels()[7].getTimeSinceLast(1).toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_2", music->syncChannels()[7].getTimeSinceLast(2).toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_S_3", music->syncChannels()[7].getTimeSinceLast(3).toSeconds()));

            vector.push_back(ojstd::make_shared<Uniform1f>("C_7_T", static_cast<float>(music->syncChannels()[7].getTotalHits())));

            return vector;
        });

        auto radialBlur = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "common/radial_blur.fs");
        radialBlur->setInputs(borgilaHyperSpace);

        // auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/blur1.fs");
        // blur1->setInputs(radialBlur);
        // blur1->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
        //     return { ojstd::make_shared<Uniform2f>("blurDir", 1.f, 0.f) };
        // });

        // auto blur2 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/blur1.fs");
        // blur2->setInputs(blur1);
        // blur2->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
        //     return { ojstd::make_shared<Uniform2f>("blurDir", 0.f, 1.f) };
        // });

        auto chrom = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/chrom_ab.fs");
        chrom->setInputs(radialBlur);
        chrom->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();

            vector.push_back(ojstd::make_shared<Uniform1f>("C_1_S", music->syncChannels()[1].getTimeSinceAnyNote().toSeconds()));

            return vector;
        });

        scenes.emplace_back(chrom, Duration::seconds(9), "borgila_hyperspace");
    }

    // Ufo landing part 2
    {
        auto noise = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/noise.fs");
        noise->setRenderOnce(true);

        auto stars = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/stars.fs");
        stars->setRenderOnce(true);

        auto ufoLanding = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/ufo_landing.fs");
        ufoLanding->setInputs(noise, stars);

        ufoLanding->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
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

            vector.push_back(ojstd::make_shared<Uniform1f>("C_3_S", music->syncChannels()[3].getTimeSinceAnyNote().toSeconds()));

            vector.push_back(ojstd::make_shared<Uniform1f>("scenePart", 2.0f));

            return vector;
        });

        ufoLanding->setTextureCallback([this]([[maybe_unused]] float relativeSceneTime) {
            ojstd::vector<ojstd::shared_ptr<Uniform1t>> vector;
            vector.push_back(ojstd::make_shared<Uniform1t>("borgilaTexture", this->getText("BORGILA", "Arial Black")));
            return vector;
        });

        auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/blur1.fs");
        blur1->setInputs(ufoLanding);
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

        scenes.emplace_back(chrom, Duration::seconds(11), "ufo_landing_part_2");
    }

    return scenes;
}

ojstd::string Edison2025::getTitle() const
{
    return "Aurora Borgilis";
}

void Edison2025::update(const Duration& relativeSceneTime, const Duration& elapsedTime, const ojstd::string& currentScene) const
{
    OJ_UNUSED(relativeSceneTime);
    OJ_UNUSED(elapsedTime);
    OJ_UNUSED(currentScene);
    const float currentTime = relativeSceneTime.toSeconds<float>();
    previousTimes[previousTimeIndex] = currentTime;
    previousTimeIndex++;
    previousTimeIndex = previousTimeIndex % 2;
    for (size_t i = 0; i < 2; i++) {
        previousTimes[previousTimeIndex] = ojstd::clamp(previousTimes[previousTimeIndex], currentTime - 0.5f, currentTime);
    }

    auto& camera = FreeCameraController::instance();

    if (currentScene == "borgila") {
        if (currentTime < 9.0) {
            camera.set({ 8.28524f - 0.4f * currentTime, 3.61f, 2.728f }, 0.7f, 0.0219999f);
        } else if (currentTime < 15.0) {
            camera.set({ -106.815f + 0.3f * currentTime, 11.84f, -9.83466f + 0.3f * currentTime }, -0.976f, -0.0420001f);
        } else if (currentTime < 30.0) {
            float t = currentTime - 30.0f;
            camera.set({ -19.6426f, 3.28002f - 0.1f * t, -2.09492f }, 0.776f, -0.08f);
        }
    } else if (currentScene == "indoor") {
        Vector3f cameraPosition { 21.9963f, 3.94f, -72.8188f };
        float speed = 1.0f;
        float heading = -3.1415f;
        float elevation = -0.22f;
        Vector3f dv { speed * ojstd::sin(heading), 0.0, -speed * ojstd::cos(heading) * currentTime };
        cameraPosition += dv;
        if (currentTime > 9.0f) {
            float s = ojstd::smoothstep(9.0, 14.0, currentTime);
            cameraPosition.x -= 1.75f * s;
            cameraPosition.y -= 0.5f * s;
            cameraPosition.z += 2.9f * s;
            elevation -= 0.4f * s;
        }
        if (currentTime > 19.0f) {
            float s = ojstd::smoothstep(19.0, 21.0, currentTime);
            elevation += 0.7f * s;
            heading -= 0.25f * s;
        }
        if (currentTime < 30.0) {
            camera.set(cameraPosition, heading, elevation);
        }
    }
}

}
