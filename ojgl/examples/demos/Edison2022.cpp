#include "Edison2022.h"
#include "FreeCameraController.h"
#include "TextRenderer.hpp"
#include "music/Music.h"
#include "utility/Log.h"
#include "utility/Spline3.h"

using namespace ojgl;

static const unsigned char song[] = {
#include "songs/edison_2022_song.inc"
};

const unsigned char* Edison2022::getSong() const
{
    return song;
}

ojstd::shared_ptr<Texture> Edison2022::getText(const ojstd::string& text) const
{
    if (!this->_textures.contains(text))
        this->_textures[text] = TextRenderer::instance().get(text);

    return this->_textures[text];
}

Edison2022::Edison2022()
{
    // FreeCameraController::instance().set({ 39.0531f, 50.1299f, 20.5951f }, 1.114f, -0.846f);

    //FreeCameraController::instance().set({ 0, 0, 30 }, { 0.f, 0.f, 0.f });
    //FreeCameraController::instance().set({ 85.5, 81.9, -63 }, -4.0f, -0.674f);

    // experiment.fs
    FreeCameraController::instance().set({ 30.17f, 23.19f, 34.3f }, 2.548f, -0.374f);
    FreeCameraController::instance().set({ 31.64f, 62.6f, -23.58f }, 2.272f, -1.066f);
    FreeCameraController::instance().set({ 31.64f, 62.6f, -23.58f }, 2.272f, -1.066f);

    // ascent.fs
    //FreeCameraController::instance().set({ 0.f, 85.5f, 90.74f }, 0.f, -0.77f);
}

// clang-format off
auto ts = {0.0f, 15.0f, 25.0f, };
float ps_x[] = {39.0531f, 7.105427357601002e-16f, 0.3502011999999999f, -0.017112168888888887f, 60.0948f, -1.0446780000000004f, -0.41984639999999995f, 0.03147202f, };
float ps_y[] = {50.1299f, 2.2204460492503132e-17f, -0.012499400000000018f, 0.0005044044444444456f, 49.0199f, -0.03450899999999969f, 0.010198800000000036f, -0.0005648900000000034f, };
float ps_z[] = {0.0f, 0.0f, -0.060000000000000005f, 0.004f, 0.0f, 0.9f, 0.12000000000000001f, -0.011000000000000001f, };
// clang-format on

ojstd::vector<Polynomial3> polysX = polynomialLoad(ps_x);
ojstd::vector<Polynomial3> polysY = polynomialLoad(ps_y);
ojstd::vector<Polynomial3> polysZ = polynomialLoad(ps_z);

Spline3 spline = Spline3(ts, polysX, polysY, polysZ);

ojstd::vector<Scene> Edison2022::buildSceneGraph(const Vector2i& sceneSize) const
{
    ojstd::vector<Scene> scenes;
    {
        auto raymarch = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/ship.fs");
        raymarch->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();

            vector.push_back(ojstd::make_shared<Uniform1f>("C_2_SINCE", music->syncChannels()[2].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_3_SINCE", music->syncChannels()[3].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_4_SINCE", music->syncChannels()[4].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_4_TOTAL", music->syncChannels()[4].getTotalHits()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_8_SINCE", music->syncChannels()[8].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_9_SINCE", music->syncChannels()[9].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_6_SINCE", music->syncChannels()[6].getTimeSinceAnyNote().toSeconds()));
            vector.push_back(ojstd::make_shared<Uniform1f>("C_6_TOTAL", music->syncChannels()[6].getTotalHits()));

            return vector;
        });

        //auto fxaa = Buffer::construct(sceneSize.x, sceneSize.y, "common/fxaa.vs", "common/fxaa.fs");
        //fxaa->setInputs(raymarch);

        auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/blur1.fs");
        blur1->setInputs(raymarch);
        blur1->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
            return { ojstd::make_shared<Uniform2f>("blurDir", 1.f, 0.f) };
        });

        auto blur2 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/blur1.fs");
        blur2->setInputs(blur1);
        blur2->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
            return { ojstd::make_shared<Uniform2f>("blurDir", 0.f, 1.f) };
        });

        auto chrom = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/ship_chrom_ab.fs");
        chrom->setInputs(blur2);
        chrom->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();

            vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_4_SINCE", music->syncChannels()[4].getTimeSinceAnyNote().toSeconds()));

            return vector;
        });

        scenes.emplace_back(chrom, Duration::seconds(12 + 10 + 15 + 10 + 16), "sceneShip");
    }

    {
        auto raymarch = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/intro.fs");
        raymarch->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            return vector;
        });

        raymarch->setTextureCallback([this]([[maybe_unused]] float relativeSceneTime) {
            ojstd::vector<ojstd::shared_ptr<Uniform1t>> vector;
            vector.push_back(ojstd::make_shared<Uniform1t>("ojText", this->getText("O J")));
            return vector;
        });

        auto fxaa = Buffer::construct(sceneSize.x, sceneSize.y, "common/fxaa.vs", "common/fxaa.fs");
        fxaa->setInputs(raymarch);

        auto chrom = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/ship_chrom_ab.fs");
        chrom->setInputs(fxaa);
        chrom->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();

            vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_4_SINCE", music->syncChannels()[4].getTimeSinceAnyNote().toSeconds()));

            return vector;
        });

        scenes.emplace_back(chrom, Duration::seconds(18), "scene0");
    }

    {
        auto raymarch = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/jellyfish.fs");
        raymarch->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            return vector;
        });

        //auto fxaa = Buffer::construct(sceneSize.x, sceneSize.y, "common/fxaa.vs", "common/fxaa.fs");
        //fxaa->setInputs(raymarch);

        auto blur1 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/blur1.fs");
        blur1->setInputs(raymarch);
        blur1->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
            return { ojstd::make_shared<Uniform2f>("blurDir", 1.f, 0.f) };
        });

        auto blur2 = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/blur1.fs");
        blur2->setInputs(blur1);
        blur2->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) -> Buffer::UniformVector {
            return { ojstd::make_shared<Uniform2f>("blurDir", 0.f, 1.f) };
        });

        auto fxaa = Buffer::construct(sceneSize.x, sceneSize.y, "common/fxaa.vs", "common/fxaa.fs");
        fxaa->setInputs(blur2);

        //auto post = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/post.fs");
        //post->setInputs(fxaa);

        auto chrom = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/ship_chrom_ab.fs");
        chrom->setInputs(fxaa);
        chrom->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();

            vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_4_SINCE", music->syncChannels()[4].getTimeSinceAnyNote().toSeconds()));

            return vector;
        });

        scenes.emplace_back(chrom, Duration::seconds(28), "scene1");
    }

    {
        auto raymarch = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/ascent.fs");
        raymarch->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            return vector;
        });

        auto fxaa = Buffer::construct(sceneSize.x, sceneSize.y, "common/fxaa.vs", "common/fxaa.fs");
        fxaa->setInputs(raymarch);

        auto chrom = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/ship_chrom_ab.fs");
        chrom->setInputs(fxaa);
        chrom->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();

            vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_4_SINCE", music->syncChannels()[4].getTimeSinceAnyNote().toSeconds()));

            return vector;
        });

        scenes.emplace_back(chrom, Duration::seconds(34), "scene2");
    }

    {
        auto raymarch = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/grass.fs");
        raymarch->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));
            return vector;
        });

        auto fxaa = Buffer::construct(sceneSize.x, sceneSize.y, "common/fxaa.vs", "common/fxaa.fs");
        fxaa->setInputs(raymarch);

        auto chrom = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/ship_chrom_ab.fs");
        chrom->setInputs(fxaa);
        chrom->setUniformCallback([]([[maybe_unused]] float relativeSceneTime) {
            Buffer::UniformVector vector;
            vector.push_back(ojstd::make_shared<UniformMatrix4fv>("iCameraMatrix", FreeCameraController::instance().getCameraMatrix()));

            auto music = Music::instance();

            vector.push_back(ojstd::make_shared<Uniform1f>("CHANNEL_4_SINCE", music->syncChannels()[4].getTimeSinceAnyNote().toSeconds()));

            return vector;
        });

        scenes.emplace_back(chrom, Duration::seconds(999999), "scene3");
    }
    return scenes;
}

void Edison2022::update(const Duration& relativeSceneTime, const Duration& elapsedTime, const ojstd::string& currentScene) const
{
    OJ_UNUSED(elapsedTime);
    OJ_UNUSED(relativeSceneTime);
    auto& camera = FreeCameraController::instance();
    Vector3f newPosition = spline(relativeSceneTime.toSeconds());
    camera.set(newPosition, { 0.f, 0.f, 0.f });
    if (currentScene == "scene0") {
        FreeCameraController::instance().set({ 85.5, 81.9, -63 }, -4.0f, -0.674f);
    } else if (currentScene == "scene1") {
        if (relativeSceneTime.toSeconds() > 15) {
            FreeCameraController::instance().set({ 30.17f, 23.19f, 34.3f }, 2.548f, -0.374f);
        } else {
            FreeCameraController::instance().set({ 77.832f, 90.95f, -63.8f }, 2.35f, -0.054f);
        }
    } else if (currentScene == "scene2") {
        FreeCameraController::instance().set({ 31.64f, 72.6f, -23.58f }, { 0.f, 0.f, 0.f });
    } else if (currentScene == "scene3") {
        const float t = ojstd::smoothstep(5, 25, relativeSceneTime.toSeconds());
        /*const Vector3f start = { 31.64f, 62.6f, -23.58f };
        const Vector3f end = { 51.94f, 61.16f, -46.8f };
        FreeCameraController::instance().set(ojstd::lerp(start, end, t), ojstd::lerp(2.272f, 2.38f, t), ojstd::lerp(-1.066f, -0.646f, t));*/

        const Vector3f start = { 31.64f, 72.6f, -23.58f };
        const Vector3f end = { 101.64f, 102.f, -23.58f };
        const Vector3f startTarget = { 0.f, 0.f, 0.f };
        const Vector3f endTarget = { 0.f, 20.f, 0.f };

        FreeCameraController::instance().set(ojstd::lerp(start, end, t), ojstd::lerp(startTarget, endTarget, t));
    }
}

ojstd::string Edison2022::getTitle() const
{
    return "Edison 2022";
}
