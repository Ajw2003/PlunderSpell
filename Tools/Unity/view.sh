#!/usr/bin/env bash
# Play mode only. Parks the local player so their camera sits at a world point looking along a
# yaw and pitch, holds them there (kinematic), and optionally captures the game view.
# Usage: bash Tools/Unity/view.sh <x> <y> <z> <yaw> <pitch> [out.png]
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
x="$1"; y="$2"; z="$3"; yaw="$4"; pitch="$5"; out="${6:-}"

bash "$here/eval.sh" "
var p = UnityEngine.Object.FindFirstObjectByType<StateMachine.PlayerStateMachine>();
if (p == null) return \"no player\";
var cam = UnityEngine.Camera.main;
if (cam == null) return \"no camera\";
var rb = p.GetComponent<UnityEngine.Rigidbody>();
rb.isKinematic = true;
p.FaceYaw(${yaw}f);
cam.transform.localRotation = UnityEngine.Quaternion.Euler(${pitch}f, ${yaw}f, 0f);
var offset = cam.transform.position - p.transform.position;
var at = new UnityEngine.Vector3(${x}f, ${y}f, ${z}f) - offset;
rb.position = at; p.transform.position = at;
UnityEngine.Physics.SyncTransforms();
return \"camera at \" + cam.transform.position + \" facing \" + cam.transform.forward;
"
if [ -n "$out" ]; then
    sleep 1.5
    bash "$here/capture.sh" "$out"
fi
