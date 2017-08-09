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
    const double desiredFrameTimeMs = 1000.0 / 60.0;
    Window window(false);
    GLState glState(vertexShader, fragmentShader);
    Music music(song);
    music.play();

    while (true) {
        timer::Timer t;
        t.start();
        auto start = std::chrono::high_resolution_clock::now();
        window.getMessages();

        glState << Uniform1f("iGlobalTime", (GLfloat)((GetTickCount() - glState.startTime()) / 1000.0f))
                << Uniform1f("CHANNEL_12_TOTAL", (float)music.syncChannels[12].getTotalHitsPerNote(0))
                << Uniform1f("CHANNEL_13_TOTAL", (float)music.syncChannels[13].getTotalHitsPerNote(0));

        for (auto& kv : music.syncChannels) {
            auto sc = kv.second;
            std::vector<GLfloat> valuesSince;
            std::vector<GLfloat> valuesTo;

            for (int i = 0; i < sc.numNotes; i++) {
                valuesSince.push_back((GLfloat)sc.getTimeSinceLast(i));
                valuesTo.push_back((GLfloat)sc.getTimeToNext(i));
            }

            glState << Uniform1fv("CHANNEL_" + std::to_string(sc.channel) + "_TIME_SINCE", valuesSince)
                    << Uniform1fv("CHANNEL_" + std::to_string(sc.channel) + "_TIME_TO", valuesTo);
        }

        glState.render();

        music.updateSync();
        t.end();
        auto durationMs = t.time<timer::ms_t>();
        if (durationMs < desiredFrameTimeMs) {
            std::this_thread::sleep_for(std::chrono::milliseconds((int)(desiredFrameTimeMs)-durationMs));
        }
    }

    return 0;
}