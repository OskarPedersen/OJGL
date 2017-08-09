#include "SyncChannel.h"
#include <iostream>
#include <numeric>
#include <stdexcept>

SyncChannel::SyncChannel()
    : _minNote(-1)
    , channel(-1)
    , numNotes(-1)
{
}

SyncChannel::SyncChannel(int numNotes, int minNote, int channel)
    : _minNote(minNote)
    , channel(channel)
    , numNotes(numNotes)
{
    _lastTimePerNote.resize(numNotes);
    _timesPerNote.resize(numNotes);
    _totalHitsPerNote.resize(numNotes);
}

SyncChannel::~SyncChannel()
{
}

void SyncChannel::pushNote(int absoluteNote, int time)
{
    _timesPerNote[absoluteNote - _minNote].push(time);
}

void SyncChannel::tick(int currentTime)
{
    _currentTime = currentTime;
    for (int note = 0; note < numNotes; note++) {
        std::queue<int>& s = _timesPerNote[note];
        while (!s.empty() && s.front() <= _currentTime) {
            _lastTimePerNote[note] = _currentTime;
            _totalHitsPerNote[note]++;
            s.pop();
        }
    }
}

float SyncChannel::getTimeToNext(int relativeNote) const
{
    const std::queue<int>& times = _timesPerNote[relativeNote];
    if (times.empty())
        return std::numeric_limits<float>::max();
    return (float)(times.front() - _currentTime);
}

float SyncChannel::getTimeSinceLast(int relativeNote) const
{
    if (_totalHitsPerNote[relativeNote] == 0)
        return std::numeric_limits<float>::max();
    return (float)(_currentTime - _lastTimePerNote[relativeNote]);
}

int SyncChannel::getTotalHitsPerNote(int relativeNote) const
{
    return _totalHitsPerNote[relativeNote];
}

int SyncChannel::getTotalHits() const
{
    return std::accumulate(_totalHitsPerNote.begin(), _totalHitsPerNote.end(), 0);
}
