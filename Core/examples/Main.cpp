#include "OJGL.h"
#include "utility\Timer.hpp"
#include <chrono>
#include <functional>
#include <memory>
#include <stdio.h>
#include <string>
#include <thread>

unsigned char song[] = {
#include "songs/song.inc"
};

std::string vertexShader{
#include "shaders/demo.vs"
};

std::string fragmentShader{
#include "shaders/demo.fs"
};

using namespace ojgl;

int main()
{

    Window window(false);
    GLState glState(vertexShader, fragmentShader);
    Music music(song);
    music.play();

    while (true) {
        timer::Timer t;
        t.start();
        window.getMessages();

        glState << Uniform1f("iGlobalTime", (GLfloat)((GetTickCount() - glState.startTime()) / 1000.0f))
                << Uniform1f("CHANNEL_12_TOTAL", (float)music.syncChannels[12].getTotalHitsPerNote(0))
                << Uniform1f("CHANNEL_13_TOTAL", (float)music.syncChannels[13].getTotalHitsPerNote(0));

        for (auto& kv : music.syncChannels) {
            auto sc = kv.second;
            std::vector<GLfloat> values;

            for (int i = 0; i < sc.numNotes; i++)
                values.push_back((GLfloat)sc.getNoteVelocity(i) / 128.0);

            glState << Uniform1fv("CHANNEL_" + std::to_string(sc.channel) + "_VELOCITY", values);
        }

        glState.render();

        music.updateSync();
        t.end();
        auto dur = t.time<timer::ms_t>();
        if (dur < 1000.0 / 60.0) {
            std::this_thread::sleep_for(std::chrono::milliseconds((int)(1000.0 / 60.0) - dur));
        }
    }

    return 0;
}