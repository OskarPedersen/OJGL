#include "OJGL.h"
#define STB_IMAGE_IMPLEMENTATION
#include "EmbeddedResources.h"
#include "thirdparty\stb_image.h"
#include "utility\ShaderReader.h"
#include <fstream>
#include <functional>
#include <iostream>
#include <memory>
#include <set>
#include <sstream>
#include <streambuf>
#include <string>
#include <thread>
#include <unordered_map>

using namespace ojgl;

void buildSceneGraph(GLState& glState, int x, int y)
{
    glState.clearScenes();

    auto pre = Buffer::construct(1024, 768, "main", "demo.vs", "demo.fs");
    auto post = Buffer::construct(1024, 768, "post", "post.vs", "post.fs");
    auto DOFScene = Buffer::construct(1024, 768, "DOFScene", "demo.vs", "dofScene.fs");
    auto DOFBlur1 = Buffer::construct(1024, 768, "DOFBlur1", "demo.vs", "dofBlur1.fs", DOFScene);
    auto DOFBlur2 = Buffer::construct(1024, 768, "DOFBlur2", "demo.vs", "dofBlur2.fs", DOFBlur1);
    auto DOFFinal = Buffer::construct(1024, 768, "DOFFinal", "demo.vs", "dofFinal.fs", DOFScene, DOFBlur2, DOFBlur1);
    auto tunnel = Buffer::construct(1024, 768, "tunnel", "demo.vs", "tunnelScene.fs");
    auto base = Buffer::construct(1024, 768, "base", "demo.vs", "demo.fs");
    auto intro = Buffer::construct(x, y, "intro", "demo.vs", "introScene.fs");
    auto grave = Buffer::construct(x, y, "grave", "demo.vs", "graveScene.fs");
    auto graveFxaa = Buffer::construct(x, y, "fxaa", "fxaa.vs", "fxaa.fs", grave);
    auto gravePost = Buffer::construct(x, y, "gravePost", "demo.vs", "graveScenePost.fs", graveFxaa);
    auto room = Buffer::construct(x, y, "room", "demo.vs", "roomScene.fs");
    auto roomFxaa = Buffer::construct(x, y, "fxaa", "fxaa.vs", "fxaa.fs", room);
    auto roomPost = Buffer::construct(x, y, "roomPost", "demo.vs", "roomScenePost.fs", roomFxaa);

    auto mountainNoise = Buffer::construct(x, y, "mountainNoise", "demo.vs", "mountainNoise.fs");
    auto mountain = Buffer::construct(x, y, "mountain", "demo.vs", "mountain.fs", mountainNoise);
    auto mountainPost = Buffer::construct(x, y, "mountain", "demo.vs", "mountainPost.fs", mountain);

    glState.addScene("mountain", mountainPost, Duration::milliseconds(3000000));

    glState.addScene("introScene", intro, Duration::milliseconds(7000));
    glState.addScene("graveScene", gravePost, Duration::milliseconds(3000000));
    glState.addScene("roomScene", roomPost, Duration::milliseconds(3000000));
    glState.addScene("baseScene", base, Duration::milliseconds(3000000));
    glState.addScene("DOFScene", DOFFinal, Duration::milliseconds(30000));
    glState.addScene("tunnelScene", tunnel, Duration::milliseconds(30000));
    glState.addScene("imageScene", pre, Duration::milliseconds(30000));
}

std::tuple<int, int, int, std::unique_ptr<unsigned char, decltype(&stbi_image_free)>> readTexture(const std::string& filepath)
{
    int width = 0, height = 0, channels = 0;
    unsigned char* data = stbi_load(filepath.c_str(), &width, &height, &channels, 0);
    std::unique_ptr<unsigned char, decltype(&stbi_image_free)> dataptr(data, stbi_image_free);
    return std::make_tuple(width, height, channels, std::move(dataptr));
}

int main()
{
    int width = 1920 / 2;
    int height = 1080 / 2;
    ShaderReader::setBasePath("examples/shaders/");
    ShaderReader::preLoad("demo.vs", resources::vertex::demo);
    ShaderReader::preLoad("post.vs", resources::vertex::post);
    ShaderReader::preLoad("post.fs", resources::fragment::post);
    ShaderReader::preLoad("demo.fs", resources::fragment::demo);
    ShaderReader::preLoad("dofScene.fs", resources::fragment::dofScene);
    ShaderReader::preLoad("dofBlur1.fs", resources::fragment::dofBlur1);
    ShaderReader::preLoad("dofBlur2.fs", resources::fragment::dofBlur2);
    ShaderReader::preLoad("dofFinal.fs", resources::fragment::dofFinal);
    ShaderReader::preLoad("tunnel.fs", resources::fragment::tunnel);

    ShaderReader::preLoad("introScene.fs", resources::fragment::intro);
    ShaderReader::preLoad("graveScene.fs", resources::fragment::grave);
    ShaderReader::preLoad("fxaa.fs", resources::fragment::fxaa);
    ShaderReader::preLoad("fxaa.vs", resources::vertex::fxaa);
    ShaderReader::preLoad("graveScenePost.fs", resources::fragment::gravePost);
    ShaderReader::preLoad("roomScene.fs", resources::fragment::room);
    ShaderReader::preLoad("roomScenePost.fs", resources::fragment::roomPost);

    ShaderReader::preLoad("mountain.fs", resources::fragment::mountain);
    ShaderReader::preLoad("mountainNoise.fs", resources::fragment::mountainNoise);
    ShaderReader::preLoad("mountainPost.fs", resources::fragment::mountainPost);

    const auto desiredFrameTime = Duration::milliseconds(17);

    Window window(width, height, false);
    GLState glState;

    Music music(resources::songs::song);
    music.play();

    buildSceneGraph(glState, width, height);

    auto[textureWidth, textureHeight, channels, data] = readTexture("examples/textures/image.png");
    auto texture = Texture::construct(textureWidth, textureHeight, channels, data.get());

    glState["imageScene"]["main"] << Uniform1t("image", texture);
    glState.setStartTime(Timepoint::now());

    auto previousPrintTime = Timepoint::now();
    while (true) {
        Timer timer;
        timer.start();
        window.getMessages();

        for (auto key : window.getPressedKeys()) {
            LOG_INFO("Key pressed: " << key);
            bool timeChanged = false;

            switch (key) {
            case Window::KEY_ESCAPE:
                return 0;
#ifdef _DEBUG
            case Window::KEY_LEFT:
                glState.changeTime(Duration::milliseconds(-1000));
                timeChanged = true;
                break;

            case Window::KEY_RIGHT:
                glState.changeTime(Duration::milliseconds(1000));
                timeChanged = true;
                break;

            case Window::KEY_SPACE:
                glState.togglePause();
                if (glState.isPaused())
                    music.stop();
                timeChanged = true;
                break;

            case Window::KEY_R:
                glState.restart();
                timeChanged = true;
                break;

            case Window::KEY_UP:
                glState.nextScene();
                timeChanged = true;
                break;

            case Window::KEY_DOWN:
                glState.previousScene();
                timeChanged = true;
                break;
            }

            if (!glState.isPaused() && timeChanged)
                music.setTime(glState.elapsedTime());
#else
            }
#endif
        }

        glState << Uniform1f("iGlobalTime", glState.relativeSceneTime().toSeconds());
        glState << Uniform1f("resolutionWidth", static_cast<float>(width));
        glState << Uniform1f("resolutionHeight", static_cast<float>(height));

        /* glState["graveScene"]["gravePost"] << Uniform1f("CHANNEL_4_TO", min(music.syncChannels()[4].getTimeToNext(0).toSeconds(), music.syncChannels()[4].getTimeToNext(1).toSeconds()));
       /* glState["graveScene"]["gravePost"] << Uniform1f("CHANNEL_4_TO", min(music.syncChannels()[4].getTimeToNext(0).toSeconds(), music.syncChannels()[4].getTimeToNext(1).toSeconds()));
        glState["graveScene"]["gravePost"] << Uniform1f("CHANNEL_4_TOTAL", static_cast<float>(music.syncChannels()[4].getTotalHitsPerNote(0) + music.syncChannels()[4].getTotalHitsPerNote(1)));
        glState["graveScene"]["gravePost"] << Uniform1f("CHANNEL_11_SINCE", music.syncChannels()[11].getTimeSinceLast(0).toSeconds());
        glState["graveScene"]["grave"] << Uniform1f("CHANNEL_11_SINCE", music.syncChannels()[11].getTimeSinceLast(0).toSeconds());
        glState["graveScene"]["grave"] << Uniform1f("CHANNEL_11_TOTAL", static_cast<float>(music.syncChannels()[11].getTotalHits()));
        glState["graveScene"]["grave"] << Uniform1fv("CHANNEL_4_SINCE", { music.syncChannels()[4].getTimeSinceLast(0).toSeconds(), music.syncChannels()[4].getTimeSinceLast(1).toSeconds() });*/

        glState.render();

        if (!glState.isPaused())
            music.updateSync();

        timer.end();

        auto timeSinceLastPrint = Timepoint::now() - previousPrintTime;
        if (timeSinceLastPrint > Duration::seconds(1)) {
            LOG_INFO("Frame time: " << timer.currentTime());
            previousPrintTime = Timepoint::now();
        }
    }
    return 0;
}
