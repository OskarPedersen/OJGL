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

void buildSceneGraph(GLState& glState)
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

    auto roomScene = Buffer::construct(1200, 675, "roomScene", vertexShader, fragmentRoomScene);

    glState.addScene(Scene{ roomScene, timer::ms_t(3000000) });
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

int main()
{
    const timer::ms_t desiredFrameTime(17);

    Window window(1200, 675, false);
    GLState glState;

    Music music(song);
    music.play();

    buildSceneGraph(glState);

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
                glState.changeTime(timer::ms_t(-1000));
                timeChanged = true;
            }
            if (key == Window::KEY_RIGHT) {
                glState.changeTime(timer::ms_t(1000));
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
                buildSceneGraph(glState);
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

        glState[0]["roomScene"] << Uniform1f("iGlobalTime", iGlobalTime.count() / 1000.f);
        glState[1]["baseScene"] << Uniform1f("iGlobalTime", iGlobalTime.count() / 1000.f);
        /*glState[2]["tunnel"] << Uniform1f("iGlobalTime", iGlobalTime.count() / 1000.f)
                             << Uniform1f("CHANNEL_12_TOTAL", static_cast<GLfloat>(music.syncChannels[12].getTotalHitsPerNote(0)))
                             << Uniform1f("CHANNEL_13_TOTAL", static_cast<GLfloat>(music.syncChannels[13].getTotalHitsPerNote(0)));*/

        for (auto& kv : music.syncChannels) {
            auto sc = kv.second;
            std::vector<GLfloat> valuesSince;
            std::vector<GLfloat> valuesTo;

            for (int i = 0; i < sc.numNotes; i++) {
                valuesSince.push_back(static_cast<GLfloat>(sc.getTimeSinceLast(i).count()));
                valuesTo.push_back(static_cast<GLfloat>(sc.getTimeToNext(i).count()));
            }

            /*glState[2]["tunnelScene"] << Uniform1fv("CHANNEL_" + std::to_string(sc.channel) + "_TIME_SINCE", valuesSince)
                                      << Uniform1fv("CHANNEL_" + std::to_string(sc.channel) + "_TIME_TO", valuesTo);*/
        }
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
            LOG_INFO("ms: " << durationMs.count());
        }
        if (durationMs < desiredFrameTime) {
            std::this_thread::sleep_for(desiredFrameTime - durationMs);
        }
    }
    return 0;
}
