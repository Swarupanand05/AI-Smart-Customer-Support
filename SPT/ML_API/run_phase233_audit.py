"""
AI-CARE Phase 2.3.3 Comprehensive Audit & Metric Reconciliation Runner
Runs all evaluations using the canonical evaluator, validates all assertions,
and outputs exact metrics, confusion matrices, and artifacts.
"""

import os
import sys
import json
import numpy as np

# Ensure SPT/ML_API is in path
CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, CURRENT_DIR)

from app import (
    load_category_data,
    category_pipeline,
    predict_with_confidence,
    get_operational_category,
    GREETING_PHRASES
)
from sklearn.model_selection import train_test_split
from canonical_evaluator import evaluate_predictions

CANONICAL_LABELS = ['Account', 'Billing', 'General', 'Product', 'Promo', 'Returns', 'Shipping', 'Technical']

def run_audit():
    print("=" * 70)
    print("  AI-CARE Phase 2.3.3: Canonical Evaluation & Audit Runner")
    print("=" * 70)

    results = {}

    # ──────────────────────────────────────────────────────────────────────────
    # 1. CLEAN HELD-OUT SPLIT (N = 1,986)
    # ──────────────────────────────────────────────────────────────────────────
    print("\n[1/4] Evaluating Clean Held-Out Test Split (N = 1,986)...")
    c_texts, c_labels = load_category_data()
    X_tr_c, X_te_c, y_tr_c, y_te_c = train_test_split(
        c_texts, c_labels, test_size=0.15, random_state=42, stratify=c_labels
    )
    print(f"  Training samples: {len(X_tr_c)}, Test samples: {len(X_te_c)}")

    # 1a. Raw ML on Clean Held-Out
    pred_clean_raw = category_pipeline.predict(X_te_c)
    metrics_clean_raw = evaluate_predictions(
        y_te_c, pred_clean_raw, labels=CANONICAL_LABELS, dataset_name="Clean Held-Out (Raw ML)"
    )

    # 1b. Operational Pipeline on Clean Held-Out
    pred_clean_op = []
    for msg in X_te_c:
        msg_low = msg.strip().lower().rstrip(".!?,")
        if msg_low in GREETING_PHRASES or len(msg_low.split()) <= 1:
            pred_clean_op.append("General")
            continue
        cat, conf = predict_with_confidence(category_pipeline, msg)
        op_cat, _ = get_operational_category(msg, cat, conf)
        pred_clean_op.append(op_cat)

    metrics_clean_op = evaluate_predictions(
        y_te_c, pred_clean_op, labels=CANONICAL_LABELS, dataset_name="Clean Held-Out (Operational Pipeline)"
    )

    results["clean_held_out"] = {
        "raw_ml": metrics_clean_raw,
        "operational_pipeline": metrics_clean_op,
        "y_true": list(y_te_c),
        "y_pred_raw": list(pred_clean_raw),
        "y_pred_op": list(pred_clean_op)
    }

    # ──────────────────────────────────────────────────────────────────────────
    # 2. INDEPENDENT OPERATIONAL BENCHMARK (N = 160, Untouched, 20/class)
    # ──────────────────────────────────────────────────────────────────────────
    print("\n[2/4] Evaluating Independent Operational Benchmark (N = 160)...")
    indep_file = os.path.join(CURRENT_DIR, "independent_benchmark.json")
    with open(indep_file, "r", encoding="utf-8") as f:
        indep_data = json.load(f)

    i_texts = [d["text"] for d in indep_data]
    i_labels = [d["category"] for d in indep_data]

    # 2a. Raw ML on Independent Benchmark
    pred_indep_raw = category_pipeline.predict(i_texts)
    metrics_indep_raw = evaluate_predictions(
        i_labels, pred_indep_raw, labels=CANONICAL_LABELS, dataset_name="Independent Benchmark (Raw ML)"
    )

    # 2b. Operational Pipeline on Independent Benchmark
    pred_indep_op = []
    for it in i_texts:
        cat, conf = predict_with_confidence(category_pipeline, it)
        op_cat, _ = get_operational_category(it, cat, conf)
        pred_indep_op.append(op_cat)

    metrics_indep_op = evaluate_predictions(
        i_labels, pred_indep_op, labels=CANONICAL_LABELS, dataset_name="Independent Benchmark (Operational Pipeline)"
    )

    results["independent_benchmark"] = {
        "raw_ml": metrics_indep_raw,
        "operational_pipeline": metrics_indep_op,
        "texts": i_texts,
        "y_true": i_labels,
        "y_pred_raw": list(pred_indep_raw),
        "y_pred_op": list(pred_indep_op)
    }

    # ──────────────────────────────────────────────────────────────────────────
    # 3. DEVELOPMENT / REGRESSION BENCHMARK (N = 80, 10/class)
    # ──────────────────────────────────────────────────────────────────────────
    print("\n[3/4] Evaluating Development Benchmark (N = 80)...")
    dev_file = os.path.join(CURRENT_DIR, "provisional_benchmark.json")
    with open(dev_file, "r", encoding="utf-8") as f:
        dev_data = json.load(f)

    d_texts = [d["text"] for d in dev_data]
    d_labels = [d["category"] for d in dev_data]

    pred_dev_raw = category_pipeline.predict(d_texts)
    metrics_dev_raw = evaluate_predictions(
        d_labels, pred_dev_raw, labels=CANONICAL_LABELS, dataset_name="Development Benchmark (Raw ML)"
    )

    pred_dev_op = []
    for dt in d_texts:
        cat, conf = predict_with_confidence(category_pipeline, dt)
        op_cat, _ = get_operational_category(dt, cat, conf)
        pred_dev_op.append(op_cat)

    metrics_dev_op = evaluate_predictions(
        d_labels, pred_dev_op, labels=CANONICAL_LABELS, dataset_name="Development Benchmark (Operational Pipeline)"
    )

    results["development_benchmark"] = {
        "raw_ml": metrics_dev_raw,
        "operational_pipeline": metrics_dev_op,
        "texts": d_texts,
        "y_true": d_labels,
        "y_pred_raw": list(pred_dev_raw),
        "y_pred_op": list(pred_dev_op)
    }

    # ──────────────────────────────────────────────────────────────────────────
    # 4. PRINT SUMMARY TABLES & RECONCILIATION DETAILS
    # ──────────────────────────────────────────────────────────────────────────
    print("\n" + "=" * 70)
    print("  RESULTS RECONCILIATION TABLE")
    print("=" * 70)

    print("\n[Summary Comparison]")
    print(f"{'Evaluation Set':<35} | {'Raw ML Acc':<10} | {'Raw Macro-F1':<12} | {'Op Acc':<10} | {'Op Macro-F1':<12}")
    print("-" * 88)
    print(f"{'Clean Held-Out (N=1,986)':<35} | {metrics_clean_raw['accuracy']:<10.4f} | {metrics_clean_raw['macro_f1']:<12.4f} | {metrics_clean_op['accuracy']:<10.4f} | {metrics_clean_op['macro_f1']:<12.4f}")
    print(f"{'Development Benchmark (N=80)':<35} | {metrics_dev_raw['accuracy']:<10.4f} | {metrics_dev_raw['macro_f1']:<12.4f} | {metrics_dev_op['accuracy']:<10.4f} | {metrics_dev_op['macro_f1']:<12.4f}")
    print(f"{'Independent Benchmark (N=160)':<35} | {metrics_indep_raw['accuracy']:<10.4f} | {metrics_indep_raw['macro_f1']:<12.4f} | {metrics_indep_op['accuracy']:<10.4f} | {metrics_indep_op['macro_f1']:<12.4f}")

    print("\n" + "=" * 70)
    print("  INDEPENDENT BENCHMARK (N=160) DETAILED BREAKDOWN")
    print("=" * 70)
    print("\nOperational Pipeline Per-Class Performance:")
    print(f"{'Category':<12} | {'Support':<8} | {'PredCount':<10} | {'Precision':<10} | {'Recall':<8} | {'F1-Score':<8}")
    print("-" * 65)
    for cls in CANONICAL_LABELS:
        c_data = metrics_indep_op["per_class"][cls]
        print(f"{cls:<12} | {c_data['support']:<8} | {c_data['prediction_count']:<10} | {c_data['precision']:<10.4f} | {c_data['recall']:<8.4f} | {c_data['f1_score']:<8.4f}")
    print("-" * 65)
    print(f"{'Overall Macro':<12} | {metrics_indep_op['sample_count']:<8} | {metrics_indep_op['sample_count']:<10} | {metrics_indep_op['macro_precision']:<10.4f} | {metrics_indep_op['macro_recall']:<8.4f} | {metrics_indep_op['macro_f1']:<8.4f}")
    print(f"{'Overall Weighted':<12} | {metrics_indep_op['sample_count']:<8} | {metrics_indep_op['sample_count']:<10} | {metrics_indep_op['weighted_precision']:<10.4f} | {metrics_indep_op['weighted_recall']:<8.4f} | {metrics_indep_op['weighted_f1']:<8.4f}")
    print(f"{'Accuracy':<12} | {metrics_indep_op['accuracy']:.4f} ({sum(metrics_indep_op['per_class'][c]['true_positive'] for c in CANONICAL_LABELS)} / {metrics_indep_op['sample_count']})")

    print("\nOperational Pipeline Confusion Matrix (Rows = True, Columns = Predicted):")
    print(f"{'':<12} " + " ".join(f"{c[:5]:>7}" for c in CANONICAL_LABELS) + f" | {'RowSum':>6}")
    cm = np.array(metrics_indep_op["confusion_matrix"]["matrix"])
    for i, cls in enumerate(CANONICAL_LABELS):
        row_str = " ".join(f"{cm[i, j]:>7}" for j in range(len(CANONICAL_LABELS)))
        print(f"{cls:<12} {row_str} | {cm[i].sum():>6}")
    col_str = " ".join(f"{cm[:, j].sum():>7}" for j in range(len(CANONICAL_LABELS)))
    print("-" * 75)
    print(f"{'ColSum':<12} {col_str} | {cm.sum():>6}")
    print(f"Matrix Trace (Sum of Diagonal): {np.trace(cm)}")

    # Save to disk as reproducible evaluation artifact
    artifact_file = os.path.join(CURRENT_DIR, "evaluation_audit_results.json")
    with open(artifact_file, "w", encoding="utf-8") as f:
        json.dump(results, f, indent=2)
    print(f"\n[Artifact Saved] Audit results saved to: {artifact_file}")

    return results

if __name__ == "__main__":
    run_audit()
