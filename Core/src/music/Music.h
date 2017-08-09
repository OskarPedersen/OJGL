#pragma once

#include "SyncChannel.h"
#include "V2MPlayer.h"
#include <map>

namespace ojgl {

class Music {

public:
    Music(unsigned char* song);
    ~Music();
    void play();
    void updateSync();
    std::map<int, SyncChannel> syncChannels;

private:
    void initSync();
    unsigned char* _song;
    std::unique_ptr<V2MPlayer> _player;
    std::chrono::time_point<std::chrono::system_clock> _start;
};
} //namespace ojgl