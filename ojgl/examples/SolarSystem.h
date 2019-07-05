#pragma once

#include "utility/OJstd.h"

using namespace ojstd;

struct Planet {
    Planet(float mass, Vec3 pos, Vec3 vel);

    float mass;
    Vec3 pos;
    Vec3 vel;
};

class SolarSystem {
public:
    SolarSystem();
    ~SolarSystem();

    void tick(int tick);
    vector<float> getValues();
    float getMarsScale();
    void reset();

private:
    vector<Planet> planets;
    int current;
};
