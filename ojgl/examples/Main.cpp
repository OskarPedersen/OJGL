
#include "EmbeddedResources.h"
#include "render/GLState.h"
#include "render/Popup.h"
#include "render/Texture.h"
#include "render/Window.h"
#include "utility/Log.h"
#include "utility/Macros.h"
#include "utility/OJstd.h"
#include "utility/ShaderReader.h"

using namespace ojgl;

void buildSceneGraph(GLState& glState, int x, int y)
{
    glState.clearScenes();

    {
        auto edison = Buffer::construct(BufferFormat::Meshes, x, y, "intro", "shaders/mesh.vs", "shaders/mesh.fs");
        auto mountain = Buffer::construct(BufferFormat::Quad, x, y, "fxaa", "shaders/edison.vs", "shaders/lavaScene2.fs", edison);

        glState.addScene("introScene", mountain, Duration::seconds(22));
    }
    /*{
        auto noise = Buffer::construct(x, y, "intro", "shaders/demo.vs", "shaders/mountainNoise.fs");
        auto mountain = Buffer::construct(x, y, "fxaa", "shaders/demo.vs", "shaders/mountain.fs", noise);
        auto fxaa = Buffer::construct(x, y, "fxaa", "shaders/fxaa.vs", "shaders/fxaa.fs", mountain);
        auto post = Buffer::construct(x, y, "post", "shaders/demo.vs", "shaders/mountainPost.fs", fxaa);
        glState.addScene("introScene", post, Duration::seconds(77));
    }

    {
        auto edison = Buffer::construct(x, y, "intro", "shaders/edison.vs", "shaders/lavaScene2.fs");
        auto fxaa = Buffer::construct(x, y, "fxaa", "shaders/fxaa.vs", "shaders/fxaa.fs", edison);
        auto post = Buffer::construct(x, y, "post", "shaders/post.vs", "shaders/post.fs", fxaa);
        glState.addScene("introScene", post, Duration::seconds(40));
    }
    {
        auto edison = Buffer::construct(x, y, "intro", "shaders/edison.vs", "shaders/outro.fs");
        auto fxaa = Buffer::construct(x, y, "fxaa", "shaders/fxaa.vs", "shaders/fxaa.fs", edison);
        auto post = Buffer::construct(x, y, "post", "shaders/post.vs", "shaders/post.fs", fxaa);
        glState.addScene("introScene", post, Duration::seconds(40));
    }*/
}

int main(int argc, char* argv[])
{
    //auto popupData = popup::show();

    OJ_UNUSED(argc);
    OJ_UNUSED(argv);
    int width = 1920; //popupData.width;
    int height = 1080; //popupData.height;
    bool fullScreen = false; //popupData.full;
    bool showCursor = !fullScreen;

    /*#ifndef _DEBUG
    fullScreen = true;
    showMouse = false;
	#endif
    if (argc >= 3) {
        width = std::stoi(argv[1]);
        height = std::stoi(argv[2]);
        if (argc >= 4) {
            full = static_cast<bool>(std::stoi(argv[3]));
        }
    }
    */

    ShaderReader::setBasePath("examples/");
    ShaderReader::preLoad("shaders/edison.vs", resources::vertex::edison);
    ShaderReader::preLoad("shaders/demo.vs", resources::vertex::demo);
    ShaderReader::preLoad("shaders/post.vs", resources::vertex::post);
    ShaderReader::preLoad("shaders/fxaa.vs", resources::vertex::fxaa);
    ShaderReader::preLoad("shaders/fxaa.fs", resources::fragment::fxaa);
    ShaderReader::preLoad("shaders/post.fs", resources::fragment::post);
    ShaderReader::preLoad("shaders/lavaIntro.fs", resources::fragment::lavaIntro);
    ShaderReader::preLoad("shaders/mountain.fs", resources::fragment::mountain);
    ShaderReader::preLoad("shaders/mountainNoise.fs", resources::fragment::mountainNoise);
    ShaderReader::preLoad("shaders/mountainPost.fs", resources::fragment::mountainPost);
    ShaderReader::preLoad("shaders/lavaScene2.fs", resources::fragment::lavaScene2);
    ShaderReader::preLoad("shaders/outro.fs", resources::fragment::outro);
    ShaderReader::preLoad("shaders/mesh.fs", resources::fragment::mesh);
    ShaderReader::preLoad("shaders/mesh.vs", resources::vertex::mesh);

    // @todo move this into GLState? We can return a const reference to window.
    // and perhaps have a unified update() which does getMessages(), music sync update and
    // so on.
    Window window(width, height, "Eldur - OJ", fullScreen, showCursor);
    GLState glState(resources::songs::song);
    buildSceneGraph(glState, width, height);
    glState.initialize();

    ojstd::vector<float> vertices1 = {
        -0.5, 0.5, 0, -0.5, -0.5, 0, 0.5, -0.5, 0
    };

    ojstd::vector<float> vertices2 = {
        -0, 0.5, 0, -0.5, -0.5, 0, 0.5, -0.5, 0
    };

    auto mesh = Mesh::construct(vertices1);
    auto mesh2 = Mesh::construct(vertices2);

    glState["introScene"]["intro"] << mesh;
    glState["introScene"]["intro"] << mesh2;

    while (!glState.end() && !window.isClosePressed()) {
        Timer timer;
        timer.start();
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

        glState << Uniform1f("iTime", glState.relativeSceneTime().toSeconds());
        glState << Uniform1f("iGlobalTime", glState.relativeSceneTime().toSeconds() - 2.f);
        glState << Uniform2f("iResolution", static_cast<float>(width), static_cast<float>(height));
        glState << UniformMatrix4fv("M", Matrix::scaling(ojstd::sin(glState.relativeSceneTime().toSeconds())));
        glState.update();
        timer.end();
        ojstd::sleep(10); // Are OpenGL calls async?
    }
}

extern "C" int _tmain(int argc, char** argv)
{
    return main(argc, argv);
}
