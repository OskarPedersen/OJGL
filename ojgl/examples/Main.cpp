#include "EmbeddedResources.h"
#include "FreeCameraController.h"
#include "TextRenderer.hpp"
#include "demo/Demo.h"
#include "demos/Edison2025.h"
#include "render/GLState.h"
#include "render/Popup.h"
#include "render/Texture.h"
#include "render/Window.h"
#include "utility/Log.h"
#include "utility/Macros.h"
#include "utility/OJstd.h"
#include "utility/ShaderReader.h"

#ifdef RENDERDOC
#include "renderdoc_app.h"
#include <windows.h>

RENDERDOC_API_1_1_2* renderdocApi = nullptr;
#endif

using namespace ojgl;

enum class DemoType {
    DodensTriumf,
    Eldur,
    InnerSystemLab,
    QED,
    Template,
    Edison2021,
    Edison2022,
    Edison2025,
};

ojstd::shared_ptr<Demo> getDemo([[maybe_unused]] DemoType type)
{
    return ojstd::make_shared<Edison2025>();
}

int main([[maybe_unused]] int argc, [[maybe_unused]] char* argv[])
{

#ifdef RENDERDOC
    if (HMODULE mod = GetModuleHandleA("renderdoc.dll")) {
        pRENDERDOC_GetAPI RENDERDOC_GetAPI = (pRENDERDOC_GetAPI)GetProcAddress(mod, "RENDERDOC_GetAPI");
        const int ret = RENDERDOC_GetAPI(eRENDERDOC_API_Version_1_1_2, (void**)&renderdocApi);
        _ASSERTE(ret == 1);
    }
#endif
    popup::Data popupData = popup::show();

    bool saveFrames = false;
    const Vector2i windowSize(popupData.width, popupData.height);
    const bool fullScreen = popupData.full;
    const bool showCursor = !fullScreen;

    ShaderReader::setBasePath("examples/shaders/");
    for (const auto& [content, path] : resources::shaders)
        ShaderReader::preLoad(path, content);

    const auto demo = getDemo(DemoType::Edison2025);
    Window window(windowSize, demo->getTitle(), fullScreen, showCursor);
    TextRenderer::instance().setHDC(window.hdcBackend());

    GLState glState(window, demo, saveFrames);

    while (!glState.end() && !window.isClosePressed()) {
        Timer timer;
        timer.start();
#ifdef _DEBUG
        FreeCameraController::instance().update(window);

        bool captureFrame = false;
#endif
        window.getMessages();
        for (auto key : window.getPressedKeys()) {
            switch (key) {
            case Window::KEY_ESCAPE:
                return 0;
#ifdef _DEBUG
            case Window::KEY_LEFT:
                glState.changeTime(Duration::milliseconds(-5000));
                break;

            case Window::KEY_RIGHT:
                glState.changeTime(Duration::milliseconds(5000));
                break;

            case Window::KEY_SPACE:
                glState.togglePause();
                break;

            case Window::KEY_R:
                glState.restart();
                break;

            case Window::KEY_UP:
                glState.nextScene();
                break;

            case Window::KEY_DOWN:
                glState.previousScene();
                break;

            case Window::KEY_P:
                captureFrame = true;
                break;

            case Window::KEY_C:
                const FreeCameraController& c = FreeCameraController::instance();
                LOG_INFO("Camera: {" << c.position.x << "f, " << c.position.y << "f, " << c.position.z << "f}"
                                     << ", " << c.heading << "f, " << c.elevation << "f");
                break;
#endif
            }
        }

#ifdef RENDERDOC
        if (renderdocApi && captureFrame) {
            renderdocApi->StartFrameCapture(nullptr, nullptr);
        }
#endif

        glState.update();

#ifdef RENDERDOC
        if (renderdocApi && captureFrame) {
            renderdocApi->EndFrameCapture(nullptr, nullptr);
        }
#endif
        timer.end();

#ifdef _DEBUG
        ojstd::string debugTitle("Scene time: ");
        debugTitle.append(ojstd::to_string(glState.relativeSceneTime().toSeconds<long>()));
        debugTitle.append(" Total time: ");
        debugTitle.append(ojstd::to_string(glState.elapsedTime().toSeconds<long>()));
        debugTitle.append(" Current scene: ");
        debugTitle.append(glState.currentScene());
        debugTitle.append(" Frame time: ");
        debugTitle.append(ojstd::to_string(timer.elapsed().toMilliseconds<long>()));
        debugTitle.append(" ms");
        window.setTitle(debugTitle);

        // ojstd::sleep(20);
#endif
    }
}

extern "C" int _tmain(int argc, char** argv)
{
    return main(argc, argv);
}
