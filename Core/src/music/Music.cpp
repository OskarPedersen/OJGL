#include "Music.h"
#include "render/Window.h"
#include <iostream>
#include <map>
#include <set>

namespace ojgl {

Music::Music(unsigned char* song)
{
    this->_player = std::make_unique<V2MPlayer>();
    this->_song = song;
}

Music::~Music()
{
    this->_player->Close();
}

void Music::play()
{
    this->_player->Init();
    this->_player->Open(this->_song);
    this->_player->Play();
    while (this->_player->IsPlaying())
        this->_player->Tick();
    this->_player->Stop();
    initSync();
    dsInit(this->_player->RenderProxy, this->_player.get(), GetForegroundWindow());
    this->_player->Play();
    _start = std::chrono::system_clock::now();
}

void Music::initSync()
{
    std::map<int, int> channelMinNote;
    std::map<int, int> channelMaxNote;
    std::set<int> channels;
    auto& events = _player->popSyncEvents();
    for (auto& se : events) {
        int channel = se.channel;
        int note = se.note;

        channels.insert(channel);

        if (channelMinNote.find(channel) == channelMinNote.end())
            channelMinNote[channel] = (std::numeric_limits<int>::max)();
        if (channelMaxNote.find(channel) == channelMaxNote.end())
            channelMaxNote[channel] = std::numeric_limits<int>::lowest();

        channelMinNote[channel] = min(channelMinNote[channel], note);
        channelMaxNote[channel] = max(channelMaxNote[channel], note);
    }

    for (auto& c : channels) {
        int numNotes = channelMaxNote[c] - channelMinNote[c] + 1;
        std::cout << "Channel " << c << " with " << numNotes << " notes\n";
        syncChannels[c] = SyncChannel(numNotes, channelMinNote[c], c);
    }

    for (auto& se : events)
        syncChannels[se.channel].pushNote(se.note, se.time);
}

void Music::updateSync()
{
    int time = (int)(std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now() - _start).count());
    for (auto& kv : syncChannels)
        kv.second.tick(time);
}
} //namespace ojgl