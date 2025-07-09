#include "Edison2025.h"
#include "FreeCameraController.h"
#include "music/Music.h"
#include "TextRenderer.hpp"

namespace ojgl {

namespace {
    float previousTimes[] = { 0.0f, 0.0f };
    int previousTimeIndex = 0;
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

        // lissajous
        auto lissajous = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/lissajous.fs");
        lissajous->setFeedbackInputs(lissajous);

        lissajous->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<Uniform1f>("iPreviousTime", previousTimes[previousTimeIndex]));
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
            return vector;
        });

        auto experiment = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/borgila.fs");
        experiment->setInputs(noise, lissajous, radar, ojText);

        experiment->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            return vector;
        });

        experiment->setTextureCallback([this]([[maybe_unused]] float relativeSceneTime) {
            ojstd::vector<ojstd::shared_ptr<Uniform1t>> vector;
            vector.push_back(ojstd::make_shared<Uniform1t>("borgilaTexture", this->getText("BORGILA", "Arial Black")));
            return vector;
        });

        scenes.emplace_back(experiment, Duration::seconds(20), "borgila");
    }

    // indoor scene
    {
        auto noise = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/noise.fs");
        noise->setRenderOnce(true);

        // lissajous
        auto lissajous = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/lissajous.fs");
        lissajous->setFeedbackInputs(lissajous);

        lissajous->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<Uniform1f>("iPreviousTime", previousTimes[previousTimeIndex]));
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
            return vector;
        });

        auto experiment = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/indoor.fs");
        experiment->setInputs(noise, lissajous, radar, ojText);

        experiment->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            return vector;
        });

        experiment->setTextureCallback([this]([[maybe_unused]] float relativeSceneTime) {
            ojstd::vector<ojstd::shared_ptr<Uniform1t>> vector;
            vector.push_back(ojstd::make_shared<Uniform1t>("borgilaTexture", this->getText("BORGILA", "Arial Black")));
            return vector;
        });

        // TODO: Replace this with the post.fs shader for Edison 2025
        //auto post = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/post.fs"); 
         auto post = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "eldur/post.fs");
        post->setInputs(experiment);
        scenes.emplace_back(post, Duration::seconds(3000), "indoor");
    }

    // Ufo scenes
    {
        auto noise = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/noise.fs");
        noise->setRenderOnce(true);

        auto ufoScenes = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/ufo_scenes.fs");
        ufoScenes->setInputs(noise);

        ufoScenes->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
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
        blur1->setInputs(ufoScenes);
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

        scenes.emplace_back(chrom, Duration::milliseconds(static_cast<long>(1000.0 * (8.0 + 12.0 + 10.0 + 6.5 + 10.0 + 4.0))), "ufo_scenes");
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

            return vector;
        });

        auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2025/blur1.fs");
        blur1->setInputs(ufoHyperSpace);
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

        scenes.emplace_back(chrom, Duration::seconds(1000), "ufo_hyperspace");
    }

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
    const float currentTime = relativeSceneTime.toSeconds<float>();
    previousTimes[previousTimeIndex] = currentTime;
    previousTimeIndex++;
    previousTimeIndex = previousTimeIndex % 2;
    for (size_t i = 0; i < 2; i++) {
        previousTimes[previousTimeIndex] = ojstd::clamp(previousTimes[previousTimeIndex], currentTime - 0.5f, currentTime);
    }

    auto& camera = FreeCameraController::instance();

    if (currentScene == "borgila") {
        if (currentTime < 5.0) {
            camera.set({ 14.7328f, 7.39f, -6.54882f }, 0.708f, -0.0700001f);
        } else if (currentTime < 10.0) {
            camera.set({ 30.7056f, 38.64f, 29.3115f }, 0.772f, -0.550f);
        } else if (currentTime < 20.0) {
            camera.set({ -19.6426f, 3.28002f, -2.09492f }, 0.676f, -0.08f);
        }
    } else {
        Vector3f cameraPosition { 21.9963f, 3.94f, -72.8188f };
        float speed = 1.0f;
        float heading = -3.1415f;
        float elevation = -0.22f;
        Vector3f dv { speed * ojstd::sin(heading), 0.0, -speed * ojstd::cos(heading) * currentTime };
        cameraPosition += dv;
        if (currentTime > 15.0f) {
            float s = ojstd::smoothstep(15.0, 20.0, currentTime);
            cameraPosition.x -= 1.75f * s;
            cameraPosition.y -= 0.5f * s;
            cameraPosition.z += 2.9f * s;
            elevation -= 0.4f * s;
        }

        if (currentTime > 25.0f) {
            float s = ojstd::smoothstep(25.0, 27.0, currentTime);
            elevation += 0.7f * s;
            heading -= 0.25f * s;
        }
        if (currentTime < 30.0) {
            camera.set(cameraPosition, heading, elevation);
        }
    }
}

}
