#pragma once
#include <string>
namespace ojgl {

namespace resources {

    namespace fragment {

        std::string post{
#include "shaders/post.fs"
        };

        std::string fxaa{
#include "shaders/fxaa.fs"
        };

        std::string dofScene{
#include "shaders/dofScene.fs"
        };

        std::string dofBlur1{
#include "shaders/dofBlur1.fs"
        };

        std::string dofBlur2{
#include "shaders/dofBlur2.fs"
        };

        std::string dofFinal{
#include "shaders/dofFinal.fs"
        };

        std::string tunnel{
#include "shaders/tunnelScene.fs"
        };

        std::string demo{
#include "shaders/demo.fs"
        };

        std::string intro{
#include "shaders/introScene.fs"
        };

        std::string grave{
#include "shaders/graveScene.fs"
        };

        std::string gravePost{
#include "shaders/graveScenePost.fs"
        };

        std::string room{
#include "shaders/roomScene.fs"
        };

        std::string roomPost{
#include "shaders/roomScenePost.fs"
        };

        std::string mountain{
#include "shaders/mountain.fs"
        };

        std::string mountainNoise{
#include "shaders/mountainNoise.fs"
        };

        std::string mountainPost{
#include "shaders/mountainPost.fs"
        };
    } //namespace fragment

    namespace vertex {

        std::string demo{
#include "shaders/demo.vs"
        };

        std::string post{
#include "shaders/post.vs"
        };

        std::string fxaa{
#include "shaders/fxaa.vs"
        };
    } //namespace vertex

    namespace songs {

        unsigned char song[] = {
#include "songs/song.inc"
        };
    } //namespace songs
}
}
