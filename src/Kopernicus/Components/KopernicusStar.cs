/**
 * Kopernicus Planetary System Modifier
 * -------------------------------------------------------------
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301  USA
 *
 * This library is intended to be used as a plugin for Kerbal Space Program
 * which is copyright of TakeTwo Interactive. Your usage of Kerbal Space Program
 * itself is governed by the terms of its EULA, not the license above.
 *
 * https://kerbalspaceprogram.com
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using ModularFI;
using UnityEngine;

namespace Kopernicus.Components
{
    /// <summary>
    /// Implementation of the <see cref="Sun"/> API.
    /// </summary>
    public class KopernicusStar : Sun
    {
        /// <summary>
        /// A list of all stars
        /// </summary>
        public static List<KopernicusStar> Stars;

        /// <summary>
        /// The results of the latest flux calculation for each star
        /// </summary>
        public static Dictionary<String, Double> SolarFlux;


        /// <summary>
        /// A dictionary to get the <see cref="KopernicusStar"/> component using the <see cref="CelestialBody"/>.
        /// </summary>
        public static Dictionary<CelestialBody, KopernicusStar> CelestialBodies;

        /// <summary>
        /// A list of all <see cref="Sun"/><i>s</i> and their luminosity
        /// </summary>
        public static Dictionary<Sun, Double> StarsLuminosity
        {
            get
            {
                return Stars.ToDictionary(star => star as Sun, star => star.shifter.solarLuminosity);
            }
        }

        /// <summary>
        /// Color tint on light sources computed from <see cref="KopernicusSunFlare"/> that tints local light sources. White when atmospheric extinction is not enabled.
        /// </summary>
        public Color atmosphericTintCache;
        Color atmosphericTintCacheLerp;
        Color atmosphericTintCacheSunflareLerp; // Sunflare needs to come back immediately, so needs to be set to white when (0,0,0,0)

        /// <summary>
        /// The currently active <see cref="KopernicusStar"/>, for stuff we can't patch
        /// </summary>
        public static KopernicusStar Current;

        /// <summary>
        /// The SMA of the home body for purposes of SolarLuminosityAtHome calculations
        /// </summary>
        public static double HomeBodySMA;

        /// <summary>
        /// The sunlight
        /// </summary>
        public Light light;

        /// <summary>
        /// The light when we are in IVA
        /// </summary>
        public IVASun iva;

        /// <summary>
        /// The light settings for this star
        /// </summary>
        public LightShifter shifter;

        /// <summary>
        /// The <see cref="SunFlare"/> component that controls the lensflare assigned to this star
        /// </summary>
        public KopernicusSunFlare lensFlare;

        /// <summary>
        /// A cache of base.name to avoid string allocations
        /// </summary>
        public string StarName;

        /// <summary>
        /// Returns the brightest star near the given body.
        /// </summary>
        public static KopernicusStar GetBrightest(CelestialBody body)
        {
            double greatestLuminosity = 0;
            KopernicusStar BrightestStar = null;
            for (Int32 i = 0; i < KopernicusStar.Stars.Count; i++)
            {
                KopernicusStar star = KopernicusStar.Stars[i];
                double aparentLuminosity = 0;
                if ((star.shifter.givesOffLight) && (star.shifter.solarLuminosity > 0))
                {
                    Vector3d toStar = body.position - star.sun.position;
                    double distanceSq = Vector3d.SqrMagnitude(toStar);
                    aparentLuminosity = star.shifter.solarLuminosity * (1 / distanceSq);
                }
                if (aparentLuminosity > greatestLuminosity)
                {
                    greatestLuminosity = aparentLuminosity;
                    BrightestStar = star;
                }
            }
            return BrightestStar;
        }


        /// <summary>
        /// Starts up fi instance
        /// </summary>
        protected override void Awake()
        {
            if (Stars == null)
            {
                Stars = new List<KopernicusStar>();
            }

            if (SolarFlux == null)
            {
                SolarFlux = new Dictionary<String, Double>();
            }

            Stars.Add(this);
            DontDestroyOnLoad(this);
            atmosphericTintCache = Color.white; // Default value (atmospheric extinction not enabled)
            atmosphericTintCacheLerp = Color.white;
            atmosphericTintCacheSunflareLerp = Color.white;
            light = gameObject.GetComponent<Light>();

            // Gah
            typeof(Sun).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Last(f => f.FieldType == typeof(Light)).SetValue(this, light);

            // sun flare
            Camera.onPreCull += cam =>
            {
                Vector3d scaledSpace = target.transform.position - ScaledSpace.LocalToScaledSpace(sun.position);
                sunDirection = scaledSpace.normalized;
                sunRotation = sunDirection;
                sunRotation.x = Math.Round(sunRotation.x, sunRotationPrecision);
                sunRotation.y = Math.Round(sunRotation.y, sunRotationPrecision);
                sunRotation.z = Math.Round(sunRotation.z, sunRotationPrecision);
                if (sunRotation != Vector3d.zero)
                {
                    transform.forward = sunRotation;
                }
            };

            StarName = name;
        }

        /// <summary>
        /// Create stuff when the object is live
        /// </summary>
        protected override void Start()
        {
            // Get the LightShifter
            shifter = sun.scaledBody.GetComponentsInChildren<LightShifter>(true)?[0];

            // Lensflare
            if (shifter != null)
            {
                lensFlare.sunFlare.flare = shifter.sunFlare ? shifter.sunFlare : lensFlare.sunFlare.flare;
            }

            // IVA Light
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                iva = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<IVASun>().Last(), transform, true);
                iva.sunT = transform;
            }

            // Scaled Space Light
            if (!useLocalSpaceSunLight)
            {
                return;
            }

            scaledSunLight = new GameObject("Scaledspace SunLight " + sun.name).AddComponent<Light>();
            scaledSunLight.type = LightType.Directional;
            scaledSunLight.intensity = light.intensity;
            scaledSunLight.color = light.color;
            Transform scaledLightTransform = scaledSunLight.transform;
            scaledLightTransform.parent = transform;
            scaledLightTransform.localPosition = Vector3.zero;
            scaledLightTransform.localRotation = Quaternion.identity;
            scaledSunLight.cullingMask = 1 << 10;
            GameEvents.onGameSceneLoadRequested.Add(SceneLoaded);
        }

        /// <summary>
        /// Updates the light values based on the current scene
        /// </summary>
        private void SceneLoaded(GameScenes scene)
        {
            light.shadowBias = scene != GameScenes.SPACECENTER ? shadowBiasFlight : shadowBiasSpaceCentre;
            if (gameObject.GetComponentInChildren<IVASun>() != null)
            {
                DestroyImmediate(gameObject.GetComponentInChildren<IVASun>().gameObject);
            }
        }

        /// <summary>
        /// Updates this instance
        /// </summary>
        [SuppressMessage("ReSharper", "ArrangeStaticMemberQualifier")]
        private void LateUpdate()
        {
            // Set precision
            sunRotationPrecision = MapView.MapIsEnabled ? sunRotationPrecisionMapView : sunRotationPrecisionDefault;

            float lerpFactor = 1f / (RuntimeUtility.RuntimeUtility.KopernicusConfig.SolarRefreshRate * 20f + 1f);
            // Blend colours to look nicer
            if (atmosphericTintCache != Color.black)
                atmosphericTintCacheSunflareLerp = Color.Lerp(atmosphericTintCacheSunflareLerp, atmosphericTintCache, lerpFactor);
            atmosphericTintCacheLerp = Color.Lerp(atmosphericTintCacheLerp, atmosphericTintCache, lerpFactor);

            // Apply light settings
            Vector3d localSpace = ScaledSpace.ScaledToLocalSpace(target.position);
            if (light)
            {
                light.color = shifter.sunlightColor * atmosphericTintCacheLerp; // Local tint
                light.intensity =
                    shifter.intensityCurve.Evaluate((Single)Vector3d.Distance(sun.position, localSpace));
                light.shadowStrength = shifter.sunlightShadowStrength;
            }

            // Patch the ScaledSpace light
            if (scaledSunLight)
            {
                scaledSunLight.color = shifter.scaledSunlightColor; // No scaled space tint (applies only locally)
                scaledSunLight.intensity = shifter.scaledIntensityCurve.Evaluate(
                    (Single)Vector3d.Distance(ScaledSpace.LocalToScaledSpace(sun.position), target.position));
            }

            if (HighLogic.LoadedSceneIsFlight && iva && iva.GetComponent<Light>())
            {
                iva.GetComponent<Light>().color = shifter.ivaSunColor * atmosphericTintCacheLerp; // Local tint
                iva.GetComponent<Light>().intensity =
                    shifter.ivaIntensityCurve.Evaluate((Single)Vector3d.Distance(sun.position, localSpace));
            }

            // Set SunFlare color + tint
            lensFlare.sunFlare.color = shifter.sunLensFlareColor * (atmosphericTintCache == Color.black ? Color.white : atmosphericTintCacheSunflareLerp);

            // Set other stuff
            lensFlare.AU = shifter.au;
            lensFlare.brightnessCurve = shifter.brightnessCurve.Curve;
            lensFlare.sun = sun;
            lensFlare.target = target;

            // States
            Boolean lightsOn = HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium ||
                               HighLogic.LoadedScene == GameScenes.SPACECENTER;
            light.enabled = shifter.givesOffLight && lightsOn;
            lensFlare.gameObject.SetActive(shifter.givesOffLight && lightsOn);
            sunFlare.enabled = false;
            if (useLocalSpaceSunLight && Instance.useLocalSpaceSunLight)
            {
                scaledSunLight.enabled = shifter.givesOffLight && lightsOn;
            }

            if (Current != null && Current.lensFlare != null)
            {
                SunFlare.Instance = Current.lensFlare;
            }

            // Update Scaled Space Light
            if (!useLocalSpaceSunLight)
            {
                return;
            }

            if (FlightGlobals.currentMainBody == null || FlightGlobals.currentMainBody == sun)
            {
                localTime = 1f;
            }
            else
            {
                Double targetAltitude = FlightGlobals.getAltitudeAtPos(localSpace, FlightGlobals.currentMainBody);
                if (targetAltitude < 0)
                {
                    targetAltitude = 0;
                }

                Double horizonAngle = Math.Acos(FlightGlobals.currentMainBody.Radius /
                                                (FlightGlobals.currentMainBody.Radius + targetAltitude));
                Single horizonScalar = -Mathf.Sin((Single)horizonAngle);
                Single dayNightRatio = 1f - Mathf.Abs(horizonScalar);
                Single fadeStartAtAlt = horizonScalar + fadeStart * dayNightRatio;
                Single fadeEndAtAlt = horizonScalar - fadeEnd * dayNightRatio;
                localTime = Vector3.Dot(-FlightGlobals.getUpAxis(localSpace), transform.forward);
                light.intensity = Mathf.Lerp(0f, light.intensity,
                    Mathf.InverseLerp(fadeEndAtAlt, fadeStartAtAlt, localTime));
            }
        }

        /// <summary>
        /// Returns the <see cref="Vessel.solarFlux"/> at the given location.
        /// </summary>
        public Double CalculateFluxAt(Vessel vessel)
        {
            // Get sunVector
            Boolean directSunlight = false;
            Vector3 integratorPosition = vessel.transform.position;

            Vector3d truePosition = vessel.GetWorldPos3D();
            Vector3d sunPosition = sun.position;
            Vector3d scaledSpace = ScaledSpace.LocalToScaledSpace(integratorPosition);

            Vector3d sunVector = (sunPosition - truePosition).normalized;
            Ray ray = new Ray(scaledSpace, sunVector);

            // Get Thermal Stats
            if (vessel.mainBody.atmosphere)
            {
                if (sun == GetLocalStar(vessel.mainBody))
                {
                    FlightIntegrator FI = vessel.GetComponent<FlightIntegrator>();
                    vessel.mainBody.GetAtmoThermalStats(true, sun, sunVector, Vector3d.Dot(sunVector, vessel.upAxis), vessel.upAxis, vessel.altitude, out FI.atmosphereTemperatureOffset, out FI.bodyEmissiveFlux, out FI.bodyAlbedoFlux);
                }
            }

            // Get True Solar Flux
            Double realDistanceToSun = 1E+19d;
            double solarRad = sun.Radius;
            if (!Physics.Raycast(ray, out RaycastHit raycastHit, Single.MaxValue, ModularFlightIntegrator.SunLayerMask))
            {
                directSunlight = true;
                realDistanceToSun = (truePosition - sunPosition).magnitude - solarRad;
            }
            else if (raycastHit.transform.GetComponent<ScaledMovement>().celestialBody == sun)
            {
                realDistanceToSun = ScaledSpace.ScaleFactor * raycastHit.distance;
                directSunlight = true;
            }
            if (directSunlight)
            {
                realDistanceToSun += RuntimeUtility.RuntimeUtility.KopernicusConfig.EnableRealisticFlux ? solarRad : 0d;
                double atmosphericFraction = RuntimeUtility.RuntimeUtility.KopernicusConfig.EnablePhysicalAtmosphericExtinction ? ThermoHelper.SunlightPercentage(truePosition, this) : 1d;
                return PhysicsGlobals.SolarLuminosity / (realDistanceToSun * realDistanceToSun * 4d * 3.14159265358979d) * atmosphericFraction;
            }

            return 0;
        }

        /// <summary>
        /// Override for <see cref="FlightIntegrator.CalculateSunBodyFlux"/>
        /// </summary>
        public static void SunBodyFlux(ModularFlightIntegrator flightIntegrator)
        {
            // Set Physics
            PhysicsGlobals.SolarLuminosityAtHome = Current.shifter.solarLuminosity;
            PhysicsGlobals.SolarInsolationAtHome = Current.shifter.solarInsolation;
            CalculatePhysics();

            // Calculate but discard some values anyway since it's broken
            flightIntegrator.BaseFICalculateSunBodyFlux();

            // Ignore broken FI values
            Boolean directSunlight = false;
            Double solarFlux = 0d;
            // Calculate the values for all bodies
            foreach (KopernicusStar star in Stars)
            {
                // Set Physics
                PhysicsGlobals.SolarLuminosityAtHome = star.shifter.solarLuminosity;
                PhysicsGlobals.SolarInsolationAtHome = star.shifter.solarInsolation;

                //Useless applied per star as it is a static method.
                //CalculatePhysics();

                // Calculate Flux
                Double flux = Flux(flightIntegrator, star);

                // And save them
                if (flux > 0)
                    directSunlight = true;

                solarFlux += flux;
                if (!SolarFlux.ContainsKey(star.StarName))
                    SolarFlux.Add(star.StarName, flux);
                else
                    SolarFlux[star.StarName] = flux;
            }

            // Reapply
            flightIntegrator.Vessel.directSunlight = directSunlight;
            flightIntegrator.solarFlux = solarFlux;

            // Set Physics
            PhysicsGlobals.SolarLuminosityAtHome = Current.shifter.solarLuminosity;
            PhysicsGlobals.SolarInsolationAtHome = Current.shifter.solarInsolation;
            CalculatePhysics();
        }

        /// <summary>
        /// Small method to handle flux
        /// </summary>
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        private static Double Flux(ModularFlightIntegrator fi, KopernicusStar star)
        {
            // Nullchecks
            try
            {
                if (fi == null)
                {
                    return 0;
                }
                if (fi.Vessel == null || fi.Vessel.state == Vessel.State.DEAD || fi.CurrentMainBody == null)
                {
                    return 0;
                }
                if (star == null)
                {
                    return 0;
                }

                // Get sunVector
                Boolean directSunlight = false;
                Vector3 integratorPosition = fi.transform.position;

                Vector3d truePosition = fi.Vessel.GetWorldPos3D();
                Vector3d sunPosition = star.sun.position;
                Vector3d scaledSpace = ScaledSpace.LocalToScaledSpace(integratorPosition);

                Vector3d sunVector = (sunPosition - truePosition).normalized;
                Ray ray = new Ray(scaledSpace, sunVector);

                // Get Solar Flux
                Double realDistanceToSun = 1E+19d;
                double solarRad = star.sun.Radius;
                if (!Physics.Raycast(ray, out RaycastHit raycastHit, Single.MaxValue, ModularFlightIntegrator.SunLayerMask))
                {
                    directSunlight = true;
                    realDistanceToSun = (truePosition - sunPosition).magnitude - solarRad;
                }
                else if (raycastHit.transform.GetComponent<ScaledMovement>().celestialBody == star.sun)
                {
                    realDistanceToSun = ScaledSpace.ScaleFactor * raycastHit.distance;
                    directSunlight = true;
                }
                if (directSunlight)
                {
                    realDistanceToSun += RuntimeUtility.RuntimeUtility.KopernicusConfig.EnableRealisticFlux ? solarRad : 0d;
                    double atmosphericFraction = RuntimeUtility.RuntimeUtility.KopernicusConfig.EnablePhysicalAtmosphericExtinction ? ThermoHelper.SunlightPercentage(truePosition, star) : 1d;
                    return PhysicsGlobals.SolarLuminosity / (realDistanceToSun * realDistanceToSun * 4d * 3.14159265358979d) * atmosphericFraction;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Fixes the Calculation for Luminosity
        /// NEVER REMOVE THIS AGAIN!
        /// EVEN IF SQUAD MAKES EVERY FIELD PUBLIC AND OPENSOURCE AND WHATNOT
        /// </summary>
        private static void CalculatePhysics()
        {
            if (!FlightGlobals.ready)
            {
                return;
            }

            PhysicsGlobals.Instance.solarLuminosity =
                Math.Pow(HomeBodySMA, 2) * 4 * 3.14159265358979 *
                    PhysicsGlobals.SolarLuminosityAtHome;
        }



        /// <summary>
        /// Returns the host star cb directly from the given body.
        /// </summary>
        public static CelestialBody GetLocalStar(CelestialBody body)
        {
            while (body?.orbit?.referenceBody != null)
            {
                if (body.isStar)
                {
                    break;
                }
                body = body.orbit.referenceBody;
            }
            return body;
        }

        /// <summary>
        /// Returns the host star cb directly from the given body.
        /// </summary>
        public static CelestialBody GetNearestBodyOverSystenRoot(CelestialBody body)
        {
            while (body?.referenceBody != null)
            {
                if (body.referenceBody == FlightGlobals.Bodies[0])
                {
                    break;
                }
                body = body.referenceBody;
            }
            return body;
        }

        /// <summary>
        /// Returns the host planet directly above the current star.
        /// </summary>
        public static CelestialBody GetLocalPlanet(CelestialBody body)
        {
            while (body?.orbit?.referenceBody != null)
            {
                if (body.orbit.referenceBody.isStar)
                {
                    break;
                }
                body = body.orbit.referenceBody;
            }
            return body;
        }

        /// <summary>
        /// Override this function and use <see cref="Current"/> instead of Planetarium sun
        /// </summary>
        public override Double GetLocalTimeAtPosition(Vector3d wPos, CelestialBody cb)
        {
            Vector3d pos1 = Vector3d.Exclude(cb.angularVelocity, FlightGlobals.getUpAxis(cb, wPos));
            Vector3d pos2 = Vector3d.Exclude(cb.angularVelocity, Current.sun.position - cb.position);
#pragma warning disable 618
            Double angle = (Vector3d.Dot(Vector3d.Cross(pos2, pos1), cb.angularVelocity) < 0 ? -1 : 1) *
                           Vector3d.AngleBetween(pos1, pos2) / 6.28318530717959 + 0.5;
#pragma warning restore 618
            if (angle > Math.PI * 2)
            {
                angle -= Math.PI * 2;
            }

            return angle;
        }
    }
}
