#pragma once

#include "utility/OJstd.h"
namespace ojgl::resources {
// clang-format off
  ojstd::vector<ojstd::Pair<ojstd::string, ojstd::string>> shaders = {

          {
  #include "shaders/common/noise.fs"
          , "common/noise.fs"
          },

          {
  #include "shaders/common/primitives.fs"
          , "common/primitives.fs"
          },

          {
  #include "shaders/common/raymarch_settings.fs"
          , "common/raymarch_settings.fs"
          },

          {
  #include "shaders/common/raymarch_utils.fs"
          , "common/raymarch_utils.fs"
          },

          {
  #include "shaders/common/utils.fs"
          , "common/utils.fs"
          },

          {
  #include "shaders/common/raymarch_template.fs"
          , "common/raymarch_template.fs"
          },

          {
  #include "shaders/eldur/post.fs"
          , "eldur/post.fs"
          },

          {
  #include "shaders/common/fxaa.fs"
          , "common/fxaa.fs"
          },

          {
  #include "shaders/eldur/lavaIntro.fs"
          , "eldur/lavaIntro.fs"
          },

          {
  #include "shaders/eldur/mountain.fs"
          , "eldur/mountain.fs"
          },

          {
  #include "shaders/eldur/mountainNoise.fs"
          , "eldur/mountainNoise.fs"
          },

          {
  #include "shaders/eldur/mountainPost.fs"
          , "eldur/mountainPost.fs"
          },

          {
  #include "shaders/eldur/lavaScene2.fs"
          , "eldur/lavaScene2.fs"
          },

          {
  #include "shaders/eldur/outro.fs"
          , "eldur/outro.fs"
          },

          {
  #include "shaders/experiments/cachedGeometry.fs"
          , "experiments/cachedGeometry.fs"
          },

          {
  #include "shaders/experiments/lightning.fs"
          , "experiments/lightning.fs"
          },

          {
  #include "shaders/common/fxaa.vs"
          , "common/fxaa.vs"
          },

          {
  #include "shaders/common/quad.vs"
          , "common/quad.vs"
          },

          {
#include "shaders/common/radial_blur.fs"
          , "common/radial_blur.fs"
          },
          {
  #include "shaders/common/fade.fs"
          , "common/fade.fs"
          },
          {
#include "shaders/QED/raymarch_utils.fs"
            , "QED/raymarch_utils.fs"
          },

          {
#include "shaders/QED/raymarch_utils_tower.fs"
          , "QED/raymarch_utils_tower.fs"
          },

          {
#include "shaders/QED/blur1.fs"
          , "QED/blur1.fs"
          },

          {
#include "shaders/QED/post.fs"
          , "QED/post.fs"
          },

          {
#include "shaders/QED/rayMarch.fs"
          , "QED/rayMarch.fs"
          },

          {
#include "shaders/QED/rayMarch.vs"
          , "QED/rayMarch.vs"
          },

          {
#include "shaders/QED/sphere.fs"
          , "QED/sphere.fs"
          },

          {
#include "shaders/QED/sphere.vs"
          , "QED/sphere.vs"
          },

          {
#include "shaders/QED/tower.fs"
          , "QED/tower.fs"
          },

          {
#include "shaders/dodenstriumf/graveScene.fs"
          , "dodenstriumf/graveScene.fs"
          },

          {
#include "shaders/dodenstriumf/graveScenePost.fs"
          , "dodenstriumf/graveScenePost.fs"
          },

          {
#include "shaders/dodenstriumf/introScene.fs"
          , "dodenstriumf/introScene.fs"
          },

          {
#include "shaders/dodenstriumf/post.fs"
          , "dodenstriumf/post.fs"
          },

          {
#include "shaders/dodenstriumf/roomScene.fs"
          , "dodenstriumf/roomScene.fs"
          },

          {
#include "shaders/dodenstriumf/roomScenePost.fs"
          , "dodenstriumf/roomScenePost.fs"
          },

          {
#include "shaders/innersystemlab/edison_1.fs"
            , "innersystemlab/edison_1.fs"
          },

          {
#include "shaders/innersystemlab/edison_1_post.fs"
            , "innersystemlab/edison_1_post.fs"
          },

          {
#include "shaders/innersystemlab/edison_2.fs"
            , "innersystemlab/edison_2.fs"
          },

          {
#include "shaders/innersystemlab/edison_planets.fs"
            , "innersystemlab/edison_planets.fs"
          },

          {
#include "shaders/innersystemlab/hospital.fs"
            , "innersystemlab/hospital.fs"
          },
                          {
#include "shaders/edison2021/raymarch_settings.fs"
            , "edison2021/raymarch_settings.fs"
          },

          {
#include "shaders/edison2021/main.fs"
            , "edison2021/main.fs"
          },

          {
#include "shaders/edison2021/main2.fs"
            , "edison2021/main2.fs"
          },
          {
#include "shaders/edison2021/tunnel_base.fs"
            , "edison2021/tunnel_base.fs"
          },
          {
#include "shaders/edison2021/bird.fs"
          , "edison2021/bird.fs"
          },
          {
#include "shaders/edison2021/pipes.fs"
          , "edison2021/pipes.fs"
          },
          {
#include "shaders/edison2021/blob.fs"
          , "edison2021/blob.fs"
          },
          {
#include "shaders/edison2021/introText.fs"
          , "edison2021/introText.fs"
          },
          {
#include "shaders/edison2021/wire_cube.fs"
          , "edison2021/wire_cube.fs"
          },
          {
#include "shaders/edison2021/tunnel_transform.fs"
          , "edison2021/tunnel_transform.fs"
          },
          {
#include "shaders/edison2021/plane.fs"
          , "edison2021/plane.fs"
          },
          {
#include "shaders/edison2021/chrom_ab.fs"
          , "edison2021/chrom_ab.fs"
          },
          {
#include "shaders/edison2022/raymarch_settings.fs"
          , "edison2022/raymarch_settings.fs"
          },
          {
#include "shaders/edison2022/experiment.fs"
          , "edison2022/experiment.fs"
          },
          {
#include "shaders/edison2022/chrom_ab.fs"
          , "edison2022/chrom_ab.fs"
          },
                        {
#include "shaders/edison2022/blocks.fs"
          , "edison2022/blocks.fs"
          },
          {
#include "shaders/edison2022/grass.fs"
          , "edison2022/grass.fs"
          },
          {
#include "shaders/edison2022/seed.fs"
          , "edison2022/seed.fs"
          },
          {
#include "shaders/edison2022/jellyfish.fs"
          , "edison2022/jellyfish.fs"
          },
         {
#include "shaders/edison2022/intro.fs"
          , "edison2022/intro.fs"
          },
          {
#include "shaders/edison2022/ship.fs"
          , "edison2022/ship.fs"
          },
          {
#include "shaders/edison2022/ship_chrom_ab.fs"
          , "edison2022/ship_chrom_ab.fs"
          },
  };
// clang-format on
}
