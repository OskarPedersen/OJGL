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

    void tick();
    vector<float> getValues();

private:
    vector<Planet> planets;
};
