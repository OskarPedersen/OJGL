#include "OJGL.h"
#define STB_IMAGE_IMPLEMENTATION
#include "thirdparty\stb_image.h"
#include "utility\Log.h"
#include "utility\Timer.hpp"
#include <cassert>
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

unsigned char song[] = {
#include "songs/song.inc"
};

std::string vertexShader{
#include "shaders/demo.vs"
};

std::string fragmentShader{
#include "shaders/demo.fs"
};

std::string vertexShaderPost{
#include "shaders/post.vs"
};

std::string fragmentShaderPost{
#include "shaders/post.fs"
};

#define SHADER_FRAGMENT_DOF_SCENE "shaders/dofScene.fs"
std::string fragmentDOFScene{
#include SHADER_FRAGMENT_DOF_SCENE
};

#define SHADER_FRAGMENT_DOF_BLUR1 "shaders/dofBlur1.fs"
std::string fragmentDOFBlur1{
#include SHADER_FRAGMENT_DOF_BLUR1
};

#define SHADER_FRAGMENT_DOF_BLUR2 "shaders/dofBlur2.fs"
std::string fragmentDOFBlur2{
#include SHADER_FRAGMENT_DOF_BLUR2
};

#define SHADER_FRAGMENT_DOF_FINAL "shaders/dofFinal.fs"
std::string fragmentDOFFinal{
#include SHADER_FRAGMENT_DOF_FINAL
};

#define SHADER_FRAGMENT_TUNNEL_SCENE "shaders/tunnelScene.fs"
std::string fragmentTunnelScene{
#include SHADER_FRAGMENT_TUNNEL_SCENE
};

#define SHADER_FRAGMENT_BASE_SCENE "shaders/baseScene.fs"
std::string fragmentBaseScene{
#include SHADER_FRAGMENT_BASE_SCENE
};

#define SHADER_FRAGMENT_ROOM_SCENE "shaders/roomScene.fs"
std::string fragmentRoomScene{
#include SHADER_FRAGMENT_ROOM_SCENE
};

#define SHADER_FRAGMENT_ROOM_SCENE_POST "shaders/roomScenePost.fs"
std::string fragmentRoomScenePost{
#include SHADER_FRAGMENT_ROOM_SCENE_POST
};

#define SHADER_FRAGMENT_GRAVE_SCENE "shaders/graveScene.fs"
std::string fragmentGraveScene{
#include SHADER_FRAGMENT_GRAVE_SCENE
};

#define SHADER_FRAGMENT_GRAVE_SCENE_POST "shaders/graveScenePost.fs"
std::string fragmentGraveScenePost{
#include SHADER_FRAGMENT_GRAVE_SCENE_POST
};

#define SHADER_FRAGMENT_INTRO_SCENE "shaders/introScene.fs"
std::string fragmentIntroScene{
#include SHADER_FRAGMENT_INTRO_SCENE
};

std::string fxaaVertex{
#include "shaders/fxaa.vs"
};

std::string fxaaFragment{
#include "shaders/fxaa.fs"
};

using namespace ojgl;

#ifdef _DEBUG
void debugRereadShaderFiles()
{
    std::unordered_map<std::string*, std::string> shaders;
    shaders[&fragmentShader] = "examples/shaders/demo.fs";
    shaders[&fragmentShaderPost] = "examples/shaders/post.fs";

    shaders[&fragmentDOFScene] = "examples/" SHADER_FRAGMENT_DOF_SCENE;
    shaders[&fragmentDOFBlur1] = "examples/" SHADER_FRAGMENT_DOF_BLUR1;
    shaders[&fragmentDOFBlur2] = "examples/" SHADER_FRAGMENT_DOF_BLUR2;
    shaders[&fragmentDOFFinal] = "examples/" SHADER_FRAGMENT_DOF_FINAL;

    shaders[&fragmentTunnelScene] = "examples/" SHADER_FRAGMENT_TUNNEL_SCENE;

    shaders[&fragmentBaseScene] = "examples/" SHADER_FRAGMENT_BASE_SCENE;

    shaders[&fragmentRoomScene] = "examples/" SHADER_FRAGMENT_ROOM_SCENE;
    shaders[&fragmentRoomScenePost] = "examples/" SHADER_FRAGMENT_ROOM_SCENE_POST;

    shaders[&fragmentGraveScene] = "examples/" SHADER_FRAGMENT_GRAVE_SCENE;
    shaders[&fragmentGraveScenePost] = "examples/" SHADER_FRAGMENT_GRAVE_SCENE_POST;

    shaders[&fragmentIntroScene] = "examples/" SHADER_FRAGMENT_INTRO_SCENE;

    for (auto[stringptr, path] : shaders) {
        std::ifstream shaderFile(path);
        assert(!shaderFile.fail());

        std::stringstream buffer;
        buffer << shaderFile.rdbuf();
        std::string fileContents = buffer.str();
        std::string pre = "R\"\"(";
        std::string post = ")\"\"";

        std::string shader;
        size_t start = -1;
        while (true) {
            start = fileContents.find(pre, start + 1);
            if (start == std::string::npos) {
                break;
            }
            size_t end = fileContents.find(post, start);
            shader += fileContents.substr(start + pre.length(), end - start - pre.length());
        }

        *stringptr = shader;
    }
}
#endif

void buildSceneGraph(GLState& glState, int x, int y)
{
    glState.clearScenes();

    auto pre = Buffer::construct(1024, 768, "main", vertexShader, fragmentShader);
    auto post = Buffer::construct(1024, 768, "post", vertexShaderPost, fragmentShaderPost);

    auto DOFScene = Buffer::construct(1024, 768, "DOFScene", vertexShader, fragmentDOFScene);
    auto DOFBlur1 = Buffer::construct(1024, 768, "DOFBlur1", vertexShader, fragmentDOFBlur1, { DOFScene });
    auto DOFBlur2 = Buffer::construct(1024, 768, "DOFBlur2", vertexShader, fragmentDOFBlur2, { DOFBlur1 });
    auto DOFFinal = Buffer::construct(1024, 768, "DOFFinal", vertexShader, fragmentDOFFinal, { DOFScene, DOFBlur2, DOFBlur1 });

    auto tunnelScene = Buffer::construct(1024, 768, "tunnelScene", vertexShader, fragmentTunnelScene);

    auto baseScene = Buffer::construct(1024, 768, "baseScene", vertexShader, fragmentBaseScene);

    auto roomScene = Buffer::construct(x, y, "roomScene", vertexShader, fragmentRoomScene);
    auto roomFxaa = Buffer::construct(x, y, "roomFxaa", fxaaVertex, fxaaFragment, { roomScene });
    auto roomScenePost = Buffer::construct(x, y, "roomScenePost", vertexShader, fragmentRoomScenePost, { roomFxaa });

    auto graveScene = Buffer::construct(x, y, "graveScene", vertexShader, fragmentGraveScene);
    auto graveFxaa = Buffer::construct(x, y, "graveFxaa", fxaaVertex, fxaaFragment, { graveScene });
    auto graveScenePost = Buffer::construct(x, y, "graveScenePost", vertexShader, fragmentGraveScenePost, { graveFxaa });

    auto introScene = Buffer::construct(x, y, "introScene", vertexShader, fragmentIntroScene);

    glState.addScene(Scene{ introScene, timer::ms_t(7000) });
    glState.addScene(Scene{ graveScenePost, timer::ms_t(22500 + 15000 + 15000 + 10000) });
    glState.addScene(Scene{ roomScenePost, timer::ms_t(3000000) });

    glState.addScene(Scene{ baseScene, timer::ms_t(3000000) });
    glState.addScene(Scene{ DOFFinal, timer::ms_t(30000) });
    glState.addScene(Scene{ tunnelScene, timer::ms_t(30000) });
    glState.addScene(Scene{ pre, timer::ms_t(30000) });
}

std::tuple<int, int, int, std::unique_ptr<unsigned char, decltype(&stbi_image_free)>> readTexture(const std::string& filepath)
{
    int width = 0, height = 0, channels = 0;
    unsigned char* data = stbi_load(filepath.c_str(), &width, &height, &channels, 0);
    std::unique_ptr<unsigned char, decltype(&stbi_image_free)> dataptr(data, stbi_image_free);
    return std::make_tuple(width, height, channels, std::move(dataptr));
}

int main(int argc, char* argv[])
{
    int x = 1920;
    int y = 1080;
    bool full = false;
    if (argc >= 3) {
        x = std::stoi(argv[1]);
        y = std::stoi(argv[2]);
        if (argc >= 4) {
            full = static_cast<bool>(std::stoi(argv[3]));
        }
    }

    const timer::ms_t desiredFrameTime(17);

    Window window(x, y, full);
    GLState glState;

    Music music(song);
    music.play();

    buildSceneGraph(glState, x, y);

    auto[width, height, channels, data] = readTexture("examples/textures/image.png");
    auto texture = Texture::construct(width, height, channels, data.get());

    //glState[3]["main"] << Uniform1t("image", texture);
    glState.setStartTime(timer::clock_t::now());

    while (true) {
        timer::Timer t;
        t.start();

        window.getMessages();

        for (auto key : window.getPressedKeys()) {
            if (key == Window::KEY_ESCAPE) {
                return 0;
            }
#ifdef _DEBUG
            bool timeChanged(false);
            LOG_INFO("key: " << key);
            if (key == Window::KEY_LEFT) {
                glState.changeTime(timer::ms_t(-5000));
                timeChanged = true;
            }
            if (key == Window::KEY_RIGHT) {
                glState.changeTime(timer::ms_t(5000));
                timeChanged = true;
            }
            if (key == Window::KEY_SPACE) {
                glState.togglePause();
                if (glState.isPaused()) {
                    music.stop();
                }
                timeChanged = true;
            }

            if (key == Window::KEY_R) {
                glState.restart();
                timeChanged = true;
            }
            if (key == Window::KEY_UP) {
                glState.nextScene();
                timeChanged = true;
            }
            if (key == Window::KEY_DOWN) {
                glState.previousScene();
                timeChanged = true;
            }
            if (key == Window::KEY_F1) {
                debugRereadShaderFiles();
                buildSceneGraph(glState, x, y);
                glState.render();
            }

            if (!glState.isPaused() && timeChanged) {
                music.setTime(glState.elapsedTime());
            }

            if (timeChanged) {
                glState.render();
            }
#endif
        }

        auto iGlobalTime = glState.relativeSceneTime();

        glState[1]["graveFxaa"] << Uniform1f("resolutionWidth", static_cast<float>(x));
        glState[1]["graveFxaa"] << Uniform1f("resolutionHeight", static_cast<float>(y));
        glState[2]["roomFxaa"] << Uniform1f("resolutionWidth", static_cast<float>(x));
        glState[2]["roomFxaa"] << Uniform1f("resolutionHeight", static_cast<float>(y));

        glState[0]["introScene"] << Uniform1f("iGlobalTime", iGlobalTime.count() / 1000.f);
        glState[1]["graveScene"] << Uniform1f("iGlobalTime", iGlobalTime.count() / 1000.f);
        glState[1]["graveScenePost"] << Uniform1f("iGlobalTime", iGlobalTime.count() / 1000.f);
        glState[2]["roomScene"] << Uniform1f("iGlobalTime", iGlobalTime.count() / 1000.f);
        glState[2]["roomScenePost"] << Uniform1f("iGlobalTime", iGlobalTime.count() / 1000.f);
        glState[3]["baseScene"] << Uniform1f("iGlobalTime", iGlobalTime.count() / 1000.f);
        /*glState[2]["tunnel"] << Uniform1f("iGlobalTime", iGlobalTime.count() / 1000.f)
                             << Uniform1f("CHANNEL_12_TOTAL", static_cast<GLfloat>(music.syncChannels[12].getTotalHitsPerNote(0)))
                             << Uniform1f("CHANNEL_13_TOTAL", static_cast<GLfloat>(music.syncChannels[13].getTotalHitsPerNote(0)));*/

        for (auto& kv : music.syncChannels) {
            const auto& sc = kv.second;
            std::vector<GLfloat> valuesSince;
            std::vector<GLfloat> valuesTo;

            for (int i = 0; i < sc.numNotes; i++) {
                valuesSince.push_back(static_cast<GLfloat>(sc.getTimeSinceLast(i).count()));
                valuesTo.push_back(static_cast<GLfloat>(sc.getTimeToNext(i).count()));
            }
        }

        float m = min(music.syncChannels[4].getTimeToNext(0).count(), music.syncChannels[4].getTimeToNext(1).count());
        //std::cout << "m: " << m << "\n";
        glState[1]["graveScenePost"] << Uniform1f("CHANNEL_4_TO", m / 1000.f);
        glState[1]["graveScenePost"] << Uniform1f("CHANNEL_4_TOTAL", music.syncChannels[4].getTotalHitsPerNote(0) + music.syncChannels[4].getTotalHitsPerNote(1));
        glState[1]["graveScenePost"] << Uniform1f("CHANNEL_11_SINCE", music.syncChannels[11].getTimeSinceLast(0).count() / 1000.f);
        glState[1]["graveScene"] << Uniform1f("CHANNEL_11_SINCE", music.syncChannels[11].getTimeSinceLast(0).count() / 1000.f);
        glState[1]["graveScene"] << Uniform1f("CHANNEL_11_TOTAL", music.syncChannels[11].getTotalHits());
        std::vector<float> since;
        since.push_back(music.syncChannels[4].getTimeSinceLast(0).count() / 1000.f);
        since.push_back(music.syncChannels[4].getTimeSinceLast(1).count() / 1000.f);
        glState[1]["graveScene"] << Uniform1fv("CHANNEL_4_SINCE", since);
        // std::cout << (music.syncChannels[11].getTimeSinceLast(0).count() / 1000.f) << "\n";
        if (!glState.isPaused()) {
            glState.render();
        }

        if (!glState.isPaused()) {
            music.updateSync();
        }
        t.end();
        auto durationMs = t.time<timer::ms_t>();
        static int dbg = 0;
        if (dbg++ % 100 == 0) {
            //LOG_INFO("ms: " << durationMs.count());
            std::cout << "ms: " << durationMs.count() << "\n";
        }
        if (durationMs < desiredFrameTime) {
            //    std::this_thread::sleep_for(desiredFrameTime - durationMs);
        }
    }
    return 0;
}
