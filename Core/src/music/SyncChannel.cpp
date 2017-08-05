#include "SyncChannel.h"
#include <numeric>
#include <stdexcept>

SyncChannel::SyncChannel()
    : _decay(-1.0)
    , _minNote(-1)
    , channel(-1)
    , numNotes(-1)
{
    //throw std::logic_error("This constructor should not be called");
}

SyncChannel::SyncChannel(int numNotes, int minNote, int channel, double decay)
    : _decay(decay)
    , _minNote(minNote)
    , channel(channel)
    , numNotes(numNotes)
{
    _currentNoteVelocities.resize(numNotes);
    _totalHitsPerNote.resize(numNotes);
}

SyncChannel::~SyncChannel()
{
}

void SyncChannel::tick()
{
    for (auto& velocity : _currentNoteVelocities)
        velocity *= _decay;
}

void SyncChannel::setVelocity(int absoluteNote, double velocity)
{
    if (velocity > 1.0) {
        _totalHitsPerNote[absoluteNote - _minNote]++;
        _currentNoteVelocities[absoluteNote - _minNote] = velocity;
    }
}

double SyncChannel::getNoteVelocity(int relativeNote) const
{
    return _currentNoteVelocities[relativeNote];
}

double SyncChannel::getTotalNoteVelocity() const
{
    return std::accumulate(_currentNoteVelocities.begin(), _currentNoteVelocities.end(), 0.0);
}

int SyncChannel::getTotalHitsPerNote(int relativeNote) const
{
    return _totalHitsPerNote[relativeNote];
}

int SyncChannel::getTotalHits() const
{
    return std::accumulate(_totalHitsPerNote.begin(), _totalHitsPerNote.end(), 0);
}
