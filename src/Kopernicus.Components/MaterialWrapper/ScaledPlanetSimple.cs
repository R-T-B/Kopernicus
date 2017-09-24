// Material wrapper generated by shader translator tool
using System;
using UnityEngine;

namespace Kopernicus
{
    namespace MaterialWrapper
    {
        public class ScaledPlanetSimple : Material
        {
            // Internal property ID tracking object
            protected class Properties
            {
                // Return the shader for this wrapper
                private const String shaderName = "Terrain/Scaled Planet (Simple)";
                public static Shader shader
                {
                    get { return Shader.Find (shaderName); }
                }

                // Main Color, default = (1,1,1,1)
                public const String colorKey = "_Color";
                public Int32 colorID { get; private set; }

                // Specular Color, default = (0.5,0.5,0.5,1)
                public const String specColorKey = "_SpecColor";
                public Int32 specColorID { get; private set; }

                // Shininess, default = 0.078125
                public const String shininessKey = "_Shininess";
                public Int32 shininessID { get; private set; }

                // Base (RGB) Gloss (A), default = "white" { }
                public const String mainTexKey = "_MainTex";
                public Int32 mainTexID { get; private set; }

                // Normalmap, default = "bump" { }
                public const String bumpMapKey = "_BumpMap";
                public Int32 bumpMapID { get; private set; }

                // Opacity, default = 1
                public const String opacityKey = "_Opacity";
                public Int32 opacityID { get; private set; }

                // Resource Map (RGB), default = "black" { }
                public const String resourceMapKey = "_ResourceMap";
                public Int32 resourceMapID { get; private set; }

                // Singleton instance
                private static Properties singleton = null;
                public static Properties Instance
                {
                    get
                    {
                        // Construct the singleton if it does not exist
                        if(singleton == null)
                            singleton = new Properties();
            
                        return singleton;
                    }
                }

                private Properties()
                {
                    colorID = Shader.PropertyToID(colorKey);
                    specColorID = Shader.PropertyToID(specColorKey);
                    shininessID = Shader.PropertyToID(shininessKey);
                    mainTexID = Shader.PropertyToID(mainTexKey);
                    bumpMapID = Shader.PropertyToID(bumpMapKey);
                    opacityID = Shader.PropertyToID(opacityKey);
                    resourceMapID = Shader.PropertyToID(resourceMapKey);
                }
            }

            // Is some random material this material 
            public static Boolean UsesSameShader(Material m)
            {
                return m.shader.name == Properties.shader.name;
            }

            // Main Color, default = (1,1,1,1)
            public new Color color
            {
                get { return GetColor (Properties.Instance.colorID); }
                set { SetColor (Properties.Instance.colorID, value); }
            }

            // Specular Color, default = (0.5,0.5,0.5,1)
            public Color specColor
            {
                get { return GetColor (Properties.Instance.specColorID); }
                set { SetColor (Properties.Instance.specColorID, value); }
            }

            // Shininess, default = 0.078125
            public Single shininess
            {
                get { return GetFloat (Properties.Instance.shininessID); }
                set { SetFloat (Properties.Instance.shininessID, Mathf.Clamp(value,0.03f,1f)); }
            }

            // Base (RGB) Gloss (A), default = "white" { }
            public Texture2D mainTex
            {
                get { return GetTexture (Properties.Instance.mainTexID) as Texture2D; }
                set { SetTexture (Properties.Instance.mainTexID, value); }
            }

            public Vector2 mainTexScale
            {
                get { return GetTextureScale (Properties.mainTexKey); }
                set { SetTextureScale (Properties.mainTexKey, value); }
            }

            public Vector2 mainTexOffset
            {
                get { return GetTextureOffset (Properties.mainTexKey); }
                set { SetTextureOffset (Properties.mainTexKey, value); }
            }

            // Normalmap, default = "bump" { }
            public Texture2D bumpMap
            {
                get { return GetTexture (Properties.Instance.bumpMapID) as Texture2D; }
                set { SetTexture (Properties.Instance.bumpMapID, value); }
            }

            public Vector2 bumpMapScale
            {
                get { return GetTextureScale (Properties.bumpMapKey); }
                set { SetTextureScale (Properties.bumpMapKey, value); }
            }

            public Vector2 bumpMapOffset
            {
                get { return GetTextureOffset (Properties.bumpMapKey); }
                set { SetTextureOffset (Properties.bumpMapKey, value); }
            }

            // Opacity, default = 1
            public Single opacity
            {
                get { return GetFloat (Properties.Instance.opacityID); }
                set { SetFloat (Properties.Instance.opacityID, Mathf.Clamp(value,0f,1f)); }
            }

            // Resource Map (RGB), default = "black" { }
            public Texture2D resourceMap
            {
                get { return GetTexture (Properties.Instance.resourceMapID) as Texture2D; }
                set { SetTexture (Properties.Instance.resourceMapID, value); }
            }

            public Vector2 resourceMapScale
            {
                get { return GetTextureScale (Properties.resourceMapKey); }
                set { SetTextureScale (Properties.resourceMapKey, value); }
            }

            public Vector2 resourceMapOffset
            {
                get { return GetTextureOffset (Properties.resourceMapKey); }
                set { SetTextureOffset (Properties.resourceMapKey, value); }
            }

            public ScaledPlanetSimple() : base(Properties.shader)
            {
            }

            [Obsolete("Creating materials from shader source String is no longer supported. Use Shader assets instead.")]
            public ScaledPlanetSimple(String contents) : base(contents)
            {
                base.shader = Properties.shader;
            }

            public ScaledPlanetSimple(Material material) : base(material)
            {
                // Throw exception if this material was not the proper material
                if (material.shader.name != Properties.shader.name)
                    throw new InvalidOperationException("Type Mismatch: Terrain/Scaled Planet (Simple) shader required");
            }

        }
    }
}