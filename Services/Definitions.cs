using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DieselBundleViewer.Services
{
    public class Definitions
    {
        public static string DataDir = "Data";
        public static string[] ScriptDataExtensions =
        {
            "sequence_manager",
            "environment",
            "menu",
            "continent",
            "continents",
            "mission",
            "nav_data",
            "cover_data",
            "world",
            "world_cameras",
            "prefhud",
            "objective",
            "credits",
            "hint",
            "comment",
            "dialog",
            "dialog_index",
            "timeline",
            "action_message",
            "achievment",
            "controller_settings",
            "world_sounds",
            "blacklist"
        };

        public static string[] RawTextExtension =
        {
            "unit",
            "material_config",
            "object",
            "animation_def",
            "animation_states",
            "animation_state_machine",
            "animation_subset",
            "merged_font",
            "physic_effect",
            "post_processor",
            "scene",
            "gui",
            "effect",
            "render_template_database",
            "xml",
            "network_settings",
            "xbox_live",
            "atom_batcher_settings",
            "camera_shakes",
            "cameras",
            "decals",
            "physics_settings",
            "scenes",
            "texture_channels",
            "diesel_layers",
            "light_intensities"
        };

        public static string[] Extensions =
        {
            "achievment",
            "action_message",
            "animation",
            "animation_def",
            "animation_state_machine",
            "animation_states",
            "animation_subset",
            "atom_batcher_settings",
            "banksinfo",
            "bmfc",
            "bnk",
            "bnkinfo",
            "camera_shakes",
            "cameras",
            "cgb",
            "comment",
            "continent",
            "continents",
            "controller_settings",
            "cooked_physics",
            "cover_data",
            "credits",
            "decals",
            "dialog",
            "dialog_index",
            "diesel_layers",
            "effect",
            "environment",
            "font",
            "gui",
            "hint",
            "idstring_lookup",
            "light_intensities",
            "lua",
            "massunit",
            "material_config",
            "menu",
            "merged_font",
            "mission",
            "model",
            "movie",
            "nav_data",
            "network_settings",
            "object",
            "objective",
            "physic_effect",
            "physics_settings",
            "post_processor",
            "prefhud",
            "render_config",
            "render_template_database",
            "scene",
            "scenes",
            "sequence_manager",
            "sfap0",
            "shaders",
            "stream",
            "strings",
            "texture",
            "texture_channels",
            "tga",
            "unit",
            "world",
            "world_cameras",
            "world_setting",
            "world_sounds",
            "xbox_live",
            "xml",
            "blacklist"
        };

        public static string TypeFromExtension(string ext)
        {
            if (RawTextExtension.Contains(ext))
                return "text";
            else if (ScriptDataExtensions.Contains(ext))
                return "scriptdata";

            return ext;
        }
    }
}
