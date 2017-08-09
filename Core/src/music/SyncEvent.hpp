#pragma once

namespace ojgl {

struct SyncEvent {

    SyncEvent(int channel, int note, int velocity, int time)
        : channel(channel)
        , note(note)
        , velocity(velocity)
        , time(time){};
    ~SyncEvent(){};

    const int channel;
    const int note;
    const int velocity;
    const int time;
};
} //namespace ojgl