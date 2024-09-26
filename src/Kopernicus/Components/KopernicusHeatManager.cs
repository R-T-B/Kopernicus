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

using ModularFI;
using System;
using UnityEngine;

namespace Kopernicus.Components
{
    public static class ThermoHelper
    {
        /// <summary>
        /// Returns if ray from (0,0,0) intersects a sphere
        /// </summary>
        /// <param name="sphereRelPos">Sphere position (ray origin at 0,0,0)</param>
        /// <param name="radius">Radius of sphere</param>
        /// <param name="direction">Direction of ray (automatically normalized)</param>
        /// <returns>Whether the ray intersects the sphere</returns>
        public static bool EfficientSphereRaycast(Vector3d sphereRelPos, double radius, Vector3d direction)
        {
            Vector3d closestPos = direction * Math.Max(0d, Vector3d.Dot(direction, sphereRelPos)) / direction.sqrMagnitude;
            return (sphereRelPos - closestPos).sqrMagnitude < radius * radius;
        }

        /// <summary>
        /// Returns the closest altitude to sphere reached by a ray.
        /// </summary>
        /// <param name="sphereRelPos">Sphere position (ray origin at 0,0,0)</param>
        /// <param name="radius">Radius of sphere</param>
        /// <param name="direction">Direction of ray (automatically normalized)</param>
        /// <returns>The altitude above the surface of the sphere</returns>
        public static double ClosestPoint(Vector3d sphereRelPos, double radius, Vector3d direction)
        {
            Vector3d closestPos = direction * Math.Max(0d, Vector3d.Dot(direction, sphereRelPos)) / direction.sqrMagnitude;
            return (sphereRelPos - closestPos).magnitude - radius;
        }

        // Empirical approximations to integral for atmospheric optical density.
        /// <summary>
        /// Approximation of the Chapman function, DO NOT EDIT THIS (It took 2 days to figure out the constants and relations)
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="signedDistanceFromLowest"></param>
        /// <param name="altitude"></param>
        /// <param name="scaleHeight"></param>
        /// <returns></returns>
        public static double ApproximateChapmanFunc(double radius, double signedDistanceFromLowest, double altitude, double scaleHeight)
        {
            if (altitude / radius < -scaleHeight / radius * 3 && signedDistanceFromLowest / radius < -.06)
            {
                altitude /= scaleHeight;
                radius /= scaleHeight;
                signedDistanceFromLowest /= scaleHeight;
                double c = -altitude * altitude - 2d * altitude * radius;
                return Math.Exp((signedDistanceFromLowest * Math.Sqrt(c) + c) / radius) * Math.Sqrt(Math.Abs(.5d * radius / altitude));
            }
            double final = MagicIntegrationA(radius, altitude, scaleHeight) / (1d + Math.Exp(MagicIntegrationB(radius, altitude, scaleHeight) * -signedDistanceFromLowest / scaleHeight));
            if (radius / scaleHeight > 4.5)
                final /= Math.Exp(-0.3d * signedDistanceFromLowest / scaleHeight - radius * .1d - 1d) + 1d;
            return final;
        }

        static double MagicIntegrationB(double radius, double altitude, double scaleHeight)
        {
            radius /= scaleHeight;
            altitude /= scaleHeight;
            return 4d / (Math.Exp(radius + altitude + .9d - Math.Abs(radius + altitude)) * Math.Sqrt(Math.Abs(radius + altitude) + .65d));
        }
        static double MagicIntegrationA(double radius, double altitude, double scaleHeight)
        {
            radius /= scaleHeight;
            altitude /= scaleHeight;
            return Math.Exp(radius + .9d - Math.Abs(radius + altitude)) * Math.Sqrt(Math.Abs(radius + altitude) + .65d);
        }
        
        public static double AtmosphericOpticalDepth(CelestialBody a, Vector3d originalPos, Vector3d direction)
        {
            if (!a.atmosphere)
                return 0d;
            double dot = Vector3d.Dot(direction, a.position - originalPos);
            Vector3d closestPos = direction * dot / direction.sqrMagnitude;
            double closestDistance = (a.position - originalPos - closestPos).magnitude - a.Radius;
            if (closestDistance > a.atmosphereDepth)
                return 0d;
            double signedDistance = dot / direction.magnitude;
            if (closestDistance < 0 && signedDistance > 0)
                return double.PositiveInfinity;
            double localScaleHeight = 5d / Math.Log(a.atmospherePressureCurve.Evaluate(Mathf.Max(0, (float)closestDistance)) / a.atmospherePressureCurve.Evaluate(Mathf.Max(0, (float)closestDistance) + 5f));
            return ApproximateChapmanFunc(a.Radius, signedDistance, closestDistance, localScaleHeight) * a.atmDensityASL * .05d;
        }

        /// <summary>
        /// Optical Depth of rays passing through to reach the given position.
        /// </summary>
        /// <param name="worldSpacePos">Location of point to check</param>
        /// <param name="starToCheck">Star to check for</param>
        /// <returns>Optical Depth</returns>
        public static double OpticalDepth(Vector3d worldSpacePos, KopernicusStar starToCheck)
        {
            double finalResult = 0d;
            Vector3d delta = worldSpacePos - starToCheck.sun.position;
            foreach (CelestialBody c in FlightGlobals.Bodies)
            {
                // Body is the star, ignore
                if (c == starToCheck.sun)
                    continue;
                Vector3d planetDel = c.position - starToCheck.sun.position;
                // Body is behind us, cannot block our view.
                // Only when object is not within atmosphere
                if (!c.atmosphere || (c.position - worldSpacePos).magnitude > c.Radius + c.atmosphereDepth)
                    if (planetDel.sqrMagnitude > delta.sqrMagnitude)
                        continue;
                // Body is behind star, cannnot block star.
                if (Vector3d.Dot(planetDel, delta) < 0)
                    continue;
                // Check directly
                finalResult += AtmosphericOpticalDepth(c, worldSpacePos, -delta);
                if (finalResult > 200)
                    return double.PositiveInfinity;
            }
            return finalResult;
        }

        /// <summary>
        /// Advanced version of <see cref="DirectSunlight"/>
        /// </summary>
        /// <param name="worldSpacePos">Location of point to check</param>
        /// <param name="starToCheck">Star to check for</param>
        /// <returns>The fraction of star light flux visible.</returns>
        public static double SunlightPercentage(Vector3d worldSpacePos, KopernicusStar starToCheck)
        {
            return Math.Exp(-OpticalDepth(worldSpacePos, starToCheck));
        }

        /// <summary>
        /// Checks if there is direct sunlight exposed upon a point in space
        /// </summary>
        /// <param name="worldSpacePos">Location of point to check</param>
        /// <param name="starToCheck">Star to check for</param>
        /// <returns>Whether there is exposure from the star upon the point</returns>
        public static bool DirectSunlight(Vector3d worldSpacePos, KopernicusStar starToCheck)
        {
            Vector3d delta = worldSpacePos - starToCheck.sun.position;
            foreach (CelestialBody c in FlightGlobals.Bodies)
            {
                // Body is the star, ignore
                if (c == starToCheck.sun)
                    continue;
                Vector3d planetDel = c.position - starToCheck.sun.position;
                // Body is behind us, cannot block our view.
                if (planetDel.sqrMagnitude > delta.sqrMagnitude)
                    continue;
                // Body is behind star, cannnot block star.
                if (Vector3d.Dot(planetDel, delta) < 0)
                    continue;
                // Check directly
                if (EfficientSphereRaycast(planetDel, c.Radius, -delta))
                    return false;
            }
            return true;
        }

        public static double FluxAt(Vector3d worldSpace)
        {
            double totalFlux = 0;
            foreach (KopernicusStar s in KopernicusStar.Stars)
            {
                Vector3d sunPosition = s.sun.position;
                totalFlux += PhysicsGlobals.SolarLuminosity / ((worldSpace - sunPosition).sqrMagnitude * 4d * 3.14159265358979d) * SunlightPercentage(worldSpace, s);
            }
            return totalFlux;
        }
    }
    public static class KopernicusHeatManager
    {
        private static Double maxTemp = 0;
        private static Double sumTemp = 0;

        public static void NewTemp(Double newTemp, Boolean sum = false)
        {
            if (sum && newTemp > 0)
            {
                sumTemp += newTemp;
            }
            else if (newTemp > maxTemp)
            {
                maxTemp = newTemp;
            }
        }

        /// <summary>
        /// Override for <see cref="FlightIntegrator.CalculateBackgroundRadiationTemperature"/>
        /// </summary>
        internal static double RadiationTemperature(ModularFlightIntegrator flightIntegrator, Double baseTemp)
        {
            // Stock Behaviour
            baseTemp = UtilMath.Lerp(baseTemp, PhysicsGlobals.SpaceTemperature, flightIntegrator.DensityThermalLerp);

            // Kopernicus Heat Manager
            maxTemp = baseTemp;
            sumTemp = 0;

            Events.OnCalculateBackgroundRadiationTemperature.Fire(flightIntegrator);

            baseTemp = maxTemp + sumTemp;

            return baseTemp;
        }
    }
}
