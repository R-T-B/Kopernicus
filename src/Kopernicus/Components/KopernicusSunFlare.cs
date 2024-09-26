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
using UnityEngine;

namespace Kopernicus.Components
{
    /// <summary>
    /// Modifications for the SunFlare component
    /// </summary>
    public class KopernicusSunFlare : SunFlare
    {
        int FrameCounter = 0;

        protected override void Awake()
        {
            Camera.onPreCull += PreCull;
        }

        [SuppressMessage("ReSharper", "Unity.IncorrectMethodSignature")]
        private void PreCull(Camera camera)
        {
            Vector3d scaledSpace = target.transform.position - ScaledSpace.LocalToScaledSpace(sun.position);
            sunDirection = scaledSpace.normalized;
            if (sunDirection != Vector3d.zero)
            {
                transform.forward = sunDirection;
            }
        }

        [SuppressMessage("ReSharper", "DelegateSubtraction")]
        protected override void OnDestroy()
        {
            Camera.onPreCull -= PreCull;
            base.OnDestroy();
        }

        private void AtmosphericScattering(Vector3d cameraPosition)
        {
            FrameCounter++;
            if (FrameCounter < RuntimeUtility.RuntimeUtility.KopernicusConfig.SolarRefreshRate * 20) // Save resources
                return;
            FrameCounter = UnityEngine.Random.Range(0, 10);

            float density = (float)ThermoHelper.OpticalDepth(cameraPosition, KopernicusStar.CelestialBodies[sun]);

            float r = Mathf.Exp(.1f - density * .3f);
            float g = Mathf.Exp(.1f - density * .72f);
            float b = Mathf.Exp(.1f - density * 1.65f);

            if (float.IsNaN(r) || float.IsNaN(g) || float.IsNaN(b)) return;

            KopernicusStar.CelestialBodies[sun].atmosphericTintCache = new Color(r, g, b);
        }

        // Overload the stock LateUpdate function
        private void LateUpdate()
        {
            sunFlare.fadeSpeed = 10000f;
            Vector3d position = target.position;
            sunDirection = (position - ScaledSpace.LocalToScaledSpace(sun.position)).normalized;
            transform.forward = sunDirection;
            sunFlare.brightness = brightnessMultiplier *
                                  brightnessCurve.Evaluate(
                                      (Single)(1.0 / (Vector3d.Distance(position,
                                                           ScaledSpace.LocalToScaledSpace(sun.position)) /
                                                       (AU * ScaledSpace.InverseScaleFactor))));
            sunFlare.enabled = true;

            if (!RuntimeUtility.RuntimeUtility.KopernicusConfig.EnableVisualAtmosphericExtinction)
                return;

            Vector3d camPos = Vector3d.zero;
            if (PlanetariumCamera.fetch.target == null || HighLogic.LoadedScene != GameScenes.TRACKSTATION && HighLogic.LoadedScene != GameScenes.FLIGHT)
            {
                if (HighLogic.LoadedScene != GameScenes.SPACECENTER) // Still do for space centre (launch pad at 0,0,0 in world space coords always)
                    return;
            }
            else
                camPos = ScaledSpace.LocalToScaledSpace(PlanetariumCamera.fetch.transform.position) * -ScaledSpace.ScaleFactor;

            if (RuntimeUtility.RuntimeUtility.KopernicusConfig.EnableVisualAtmosphericExtinction)
                AtmosphericScattering(camPos);
        }
    }
}
