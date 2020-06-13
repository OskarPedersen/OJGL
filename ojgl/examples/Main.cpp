
#include "EmbeddedResources.h"
#include "FreeCameraController.h"
#include "render/GLState.h"
#include "render/Popup.h"
#include "render/Texture.h"
#include "render/Window.h"
#include "utility/Log.h"
#include "utility/Macros.h"
#include "utility/OJstd.h"
#include "utility/ShaderReader.h"

using namespace ojgl;

Vector2i calculateDimensions(float demoAspectRatio, int windowWidth, int windowHeight);
void buildSceneGraph(GLState& glState, int width, int height);

int main(int argc, char* argv[])
{
    //auto popupData = popup::show();

    OJ_UNUSED(argc);
    OJ_UNUSED(argv);
    int width = 1920;//popupData.width;
    int height = 1080; // popupData.height;
    bool fullScreen = false;
    bool showCursor = true;// !fullScreen;

    ShaderReader::setBasePath("examples/shaders/");
    ShaderReader::preLoad("edison.vs", resources::vertex::edison);
    ShaderReader::preLoad("demo.vs", resources::vertex::demo);
    ShaderReader::preLoad("post.vs", resources::vertex::post);
    ShaderReader::preLoad("fxaa.vs", resources::vertex::fxaa);
    ShaderReader::preLoad("fxaa.fs", resources::fragment::fxaa);
    ShaderReader::preLoad("post.fs", resources::fragment::post);
    ShaderReader::preLoad("lavaIntro.fs", resources::fragment::lavaIntro);
    ShaderReader::preLoad("mountain.fs", resources::fragment::mountain);
    ShaderReader::preLoad("mountainNoise.fs", resources::fragment::mountainNoise);
    ShaderReader::preLoad("mountainPost.fs", resources::fragment::mountainPost);
    ShaderReader::preLoad("lavaScene2.fs", resources::fragment::lavaScene2);
    ShaderReader::preLoad("outro.fs", resources::fragment::outro);
    ShaderReader::preLoad("mesh.vs", resources::vertex::mesh);
    ShaderReader::preLoad("mesh.fs", resources::fragment::mesh);
    ShaderReader::preLoad("cachedGeometry.fs", resources::fragment::cachedGeometry);
    ShaderReader::preLoad("lightning.fs", resources::fragment::lightning);
    ShaderReader::preLoad("fibber-reborn/tunnel.fs", resources::fragment::fibberReborn::tunnel);

    // @todo move this into GLState? We can return a const reference to window.
    // and perhaps have a unified update() which does getMessages(), music sync update and
    // so on.
    Window window(width, height, "Eldur - OJ", fullScreen, showCursor);
    GLState glState(resources::songs::song);

    auto[sceneWidth, sceneHeight] = calculateDimensions(16.0f / 9.0f, width, height);
    Vector2i viewportOffset((width - sceneWidth) / 2, (height - sceneHeight) / 2);

    buildSceneGraph(glState, sceneWidth, sceneHeight);
    glState.initialize();
    FreeCameraController cameraController;
    auto mesh = Mesh::constructCube();

    while (!glState.end() && !window.isClosePressed()) {
        Timer timer;
        timer.start();
        cameraController.update(window);
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
#endif
            }
        }

        //glState["meshScene"]["mesh"].insertMesh(mesh, Matrix::scaling(0.2f) * Matrix::rotation(1, 1, 1, glState.relativeSceneTime().toSeconds()));
        //glState["meshScene"]["mesh"].insertMesh(mesh, Matrix::scaling(0.4f) * Matrix::translation(0.3, ojstd::sin(glState.relativeSceneTime().toSeconds()), 0.0));

        glState << UniformMatrix4fv("P", Matrix::perspective(45.0f * 3.14159265f / 180.0f, static_cast<float>(sceneWidth) / sceneHeight, 0.001f, 1000.0f) * Matrix::translation(0.0, 0.0, -5.0));

        glState << Uniform1f("iTime", glState.relativeSceneTime().toSeconds());
        glState << Uniform1f("iGlobalTime", glState.relativeSceneTime().toSeconds() - 2.f);
        glState << Uniform2f("iResolution", static_cast<float>(sceneWidth), static_cast<float>(sceneHeight));
        glState << UniformMatrix4fv("iCameraMatrix", cameraController.getCameraMatrix());
        glState.update(viewportOffset);

        timer.end();

#ifdef _DEBUG
        ojstd::string debugTitle("Frame time: ");
        debugTitle.append(ojstd::to_string(timer.elapsed().toMilliseconds<long>()));
        debugTitle.append(" ms");
        window.setTitle(debugTitle);
#endif
    }
}

Vector2i calculateDimensions(float demoAspectRatio, int windowWidth, int windowHeight)
{
    float windowAspectRatio = static_cast<float>(windowWidth) / windowHeight;

    if (demoAspectRatio > windowAspectRatio) {
        return Vector2i(windowWidth, ojstd::ftoi(windowWidth / demoAspectRatio));
    } else {
        return Vector2i(ojstd::ftoi(windowHeight * demoAspectRatio), windowHeight);
    }
}

void buildSceneGraph(GLState& glState, int width, int height)
{
    glState.clearScenes();

    {
        auto tunnel = Buffer::construct(width, height, "edison.vs", "fibber-reborn/tunnel.fs");
        glState.addScene("tunnelScene", tunnel, Duration::seconds(99999));
    }
}

extern "C" int _tmain(int argc, char** argv)
{
    return main(argc, argv);
}
