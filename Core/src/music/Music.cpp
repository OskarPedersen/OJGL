#include "Music.h"
#include "render/Window.h"
#include "utility/Log.h"
#include <iostream>
#include <unordered_map>
#include <unordered_set>

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
    setTime(timer::ms_t(0));
}

void Music::_initSync()
{
    std::unordered_map<int, int> channelMinNote;
    std::unordered_map<int, int> channelMaxNote;
    std::unordered_set<int> channels;
    auto events = _player->popSyncEvents();
    for (auto& se : events) {
        int channel = se.channel;
        int note = se.note;

        channels.insert(channel);

        if (channelMinNote.find(channel) == channelMinNote.end()) {
            channelMinNote[channel] = (std::numeric_limits<int>::max)();
        }
        if (channelMaxNote.find(channel) == channelMaxNote.end()) {
            channelMaxNote[channel] = std::numeric_limits<int>::lowest();
        }

        channelMinNote[channel] = min(channelMinNote[channel], note);
        channelMaxNote[channel] = max(channelMaxNote[channel], note);
    }

    for (auto& c : channels) {
        int numNotes = channelMaxNote[c] - channelMinNote[c] + 1;
        LOG_INFO("Channel " << c << " with " << numNotes << " notes");
        syncChannels[c] = SyncChannel(numNotes, channelMinNote[c], c);
    }

    for (auto& se : events) {
        syncChannels[se.channel].pushNote(se.note, se.time);
    }
}

void Music::updateSync()
{
    auto time = timer::duration_cast<timer::ms_t>(timer::clock_t::now() - _startTime - timer::ms_t(200));
    for (auto& kv : syncChannels) {
        kv.second.tick(time);
    }
}

void Music::stop()
{
    this->_player->Stop();
}
} //namespace ojgl