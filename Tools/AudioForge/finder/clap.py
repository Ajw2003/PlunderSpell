"""CLAP (laion/clap-htsat-unfused) wrapper: audio and text to unit vectors in one shared space.

Loads from the local Hugging Face cache only (HF_HUB_OFFLINE), so nothing is ever downloaded.
Uses the GPU when torch sees one, the CPU otherwise. Audio is mono at 48 kHz, at most 10 s.
"""

import os
from math import gcd

os.environ.setdefault("HF_HUB_OFFLINE", "1")

import numpy as np
from scipy import signal

MODEL_ID = "laion/clap-htsat-unfused"
SR = 48000
MAX_S = 10.0

_state = {}


def _load():
    if not _state:
        import torch
        from transformers import ClapModel, ClapProcessor
        device = "cuda" if torch.cuda.is_available() else "cpu"
        model = ClapModel.from_pretrained(MODEL_ID).to(device).eval()
        _state.update(torch=torch, model=model, processor=ClapProcessor.from_pretrained(MODEL_ID), device=device)
    return _state


def device():
    return _load()["device"]


def resample(x, sr):
    """Mono float32 at 48 kHz."""
    x = np.asarray(x, dtype=np.float32)
    if sr != SR:
        g = gcd(sr, SR)
        x = signal.resample_poly(x, SR // g, sr // g).astype(np.float32)
    return x[: int(MAX_S * SR)]


def _unit(features):
    torch = _state["torch"]
    if not isinstance(features, torch.Tensor):
        features = features.pooler_output
    features = features / features.norm(dim=-1, keepdim=True)
    return features.float().cpu().numpy()


def embed_audio(clips, batch=16):
    """clips: list of mono 48 kHz float32 arrays (<= 10 s). Returns (n, dim) unit vectors."""
    s = _load()
    out = []
    for i in range(0, len(clips), batch):
        inputs = s["processor"](audio=[c[: int(MAX_S * SR)] for c in clips[i:i + batch]], sampling_rate=SR,
                                return_tensors="pt", padding=True)
        inputs = {k: v.to(s["device"]) for k, v in inputs.items()}
        with s["torch"].no_grad():
            out.append(_unit(s["model"].get_audio_features(**inputs)))
    return np.concatenate(out) if out else np.zeros((0, 512), dtype=np.float32)


def embed_text(texts):
    s = _load()
    inputs = s["processor"](text=list(texts), return_tensors="pt", padding=True)
    inputs = {k: v.to(s["device"]) for k, v in inputs.items()}
    with s["torch"].no_grad():
        return _unit(s["model"].get_text_features(**inputs))
