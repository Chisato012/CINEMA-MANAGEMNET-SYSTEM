from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import torch
from transformers import AutoModelForSequenceClassification, AutoTokenizer


SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_INTENT_MODEL_DIR = (SCRIPT_DIR / ".." / ".." / "Train" / "phobert" / "finetune-phobert").resolve()
DEFAULT_MOOD_MODEL_DIR = (SCRIPT_DIR / ".." / ".." / "Train" / "phobert" / "phobert-mood" / "checkpoint-1680").resolve()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Predict a label with a fine-tuned PhoBERT classifier.")
    parser.add_argument("--text", required=True)
    parser.add_argument("--task", choices=["intent", "mood"], default="intent")
    parser.add_argument("--model-dir", default=None)
    parser.add_argument("--max-length", type=int, default=96)
    parser.add_argument("--top-k", type=int, default=7)
    return parser.parse_args()


def main() -> None:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8")

    args = parse_args()
    default_model_dir = DEFAULT_MOOD_MODEL_DIR if args.task == "mood" else DEFAULT_INTENT_MODEL_DIR
    model_dir = Path(args.model_dir or default_model_dir).resolve()
    if not model_dir.exists():
        raise FileNotFoundError(f"Model directory was not found: {model_dir}")
    if not any((model_dir / name).exists() for name in ["model.safetensors", "pytorch_model.bin"]):
        raise FileNotFoundError(f"Model weights were not found in: {model_dir}")

    tokenizer = AutoTokenizer.from_pretrained(model_dir, use_fast=False)
    model = AutoModelForSequenceClassification.from_pretrained(model_dir)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model.to(device)
    model.eval()

    inputs = tokenizer(
        args.text,
        return_tensors="pt",
        truncation=True,
        padding=True,
        max_length=args.max_length,
    )
    inputs = {key: value.to(device) for key, value in inputs.items()}

    with torch.no_grad():
        logits = model(**inputs).logits
        probabilities = torch.softmax(logits, dim=-1)[0]

    predicted_id = int(torch.argmax(probabilities).item())
    id2label = {int(key): value for key, value in model.config.id2label.items()}
    top_k = max(1, min(args.top_k, len(probabilities)))
    top_values, top_indices = torch.topk(probabilities, k=top_k)

    payload = {
        "task": args.task,
        "text": args.text,
        "label": id2label[predicted_id],
        args.task: id2label[predicted_id],
        "confidence": float(probabilities[predicted_id].item()),
        "device": str(device),
        "modelDir": str(model_dir),
        "scores": [
            {
                "label": id2label[int(index.item())],
                "score": float(value.item()),
            }
            for value, index in zip(top_values, top_indices)
        ],
    }

    print(json.dumps(payload, ensure_ascii=False))


if __name__ == "__main__":
    main()
