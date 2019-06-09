#include "Duration.h"
#include "Timepoint.h"

#ifdef _DEBUG
#include <chrono>
#else // _DEBUG
#include <Windows.h>
#endif

namespace ojgl {

Timepoint& Timepoint::operator+=(const Duration& other)
{
    this->_timepoint += other._duration;
    return *this;
}

Timepoint& Timepoint::operator-=(const Duration& other)
{
    this->_timepoint -= other._duration;
    return *this;
}

Timepoint Timepoint::now()
{
#ifdef _DEBUG
    auto duration = std::chrono::system_clock::now().time_since_epoch();
    auto millis = std::chrono::duration_cast<std::chrono::milliseconds>(duration).count();
    return Timepoint(millis);
#else
    return Timepoint(GetTickCount());
#endif
}
}
