#include "Edison2022.h"
#include "FreeCameraController.h"
#include "TextRenderer.hpp"
#include "utility/Log.h"
#include "utility/Spline3.h"

using namespace ojgl;

ojstd::shared_ptr<Texture> Edison2022::getText(const ojstd::string& text) const
{
    if (!this->_textures.contains(text))
        this->_textures[text] = TextRenderer::instance().get(text);

    return this->_textures[text];
}

Edison2022::Edison2022()
{
    // FreeCameraController::instance().set({ 39.0531f, 50.1299f, 20.5951f }, 1.114f, -0.846f);

    FreeCameraController::instance().set({ 0, 0, 30 }, { 0.f, 0.f, 0.f });
    //FreeCameraController::instance().set({ 85.5, 81.9, -63 }, -4.0f, -0.674f);

    // experiment.fs
    //FreeCameraController::instance().set({ 30.17f, 23.19f, 34.3f }, 2.548f, -0.374f);
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

        auto post = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/post.fs");
        post->setInputs(fxaa);
        // scenes.emplace_back(post, Duration::seconds(9999), "scene0");
    }

    {
        auto raymarch = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/jellyfish.fs");
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

        auto post = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/post.fs");
        post->setInputs(fxaa);
        // scenes.emplace_back(post, Duration::seconds(20), "scene1");
    }

    {
        auto raymarch = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/grass.fs");
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

        auto chrom = Buffer::construct(sceneSize.x, sceneSize.y, "common/quad.vs", "edison2022/chrom_ab.fs");
        chrom->setInputs(fxaa);
        scenes.emplace_back(fxaa, Duration::seconds(250), "scene2");
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
        //        FreeCameraController::instance().set({ 10, 80, 10 }, { 10 - 0.5f, 70, 10 });
    } else if (currentScene == "scene1") {
        FreeCameraController::instance().set({ 30.17f, 23.19f, 34.3f }, 2.548f, -0.374f);
    } else if (currentScene == "scene2") {
        FreeCameraController::instance().set({ 39.0531f, 50.1299f, 20.5951f }, 1.114f, -0.846f);
    }
}

ojstd::string Edison2022::getTitle() const
{
    return "Edison 2022";
}
