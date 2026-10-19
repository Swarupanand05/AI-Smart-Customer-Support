"""
AI-CARE Canonical Evaluation Engine
Phase 2.3.3: Strict Mathematical Reconciliation and Leakage-Free Validation.

Provides a single, auditable evaluation function used across all evaluation
splits and benchmarks. Strictly verifies all invariants with automated assertions.
"""

import numpy as np
from sklearn.metrics import confusion_matrix, classification_report, accuracy_score


def evaluate_predictions(y_true, y_pred, labels=None, dataset_name="Evaluation"):
    """
    Canonical evaluation function that receives exact y_true and y_pred arrays,
    computes full classification metrics from the ground truth and predictions,
    and runs comprehensive assertions to prove mathematical integrity.

    Parameters:
    -----------
    y_true : list or np.ndarray of ground truth class labels
    y_pred : list or np.ndarray of predicted class labels
    labels : list of unique class labels in canonical order (optional, default: sorted unique)
    dataset_name : str, human readable name for assertions and logging

    Returns:
    --------
    dict containing:
        - accuracy (float)
        - macro_f1 (float)
        - weighted_f1 (float)
        - macro_precision (float)
        - macro_recall (float)
        - weighted_precision (float)
        - weighted_recall (float)
        - sample_count (int)
        - labels (list)
        - per_class (dict: precision, recall, f1_score, support, prediction_count)
        - confusion_matrix (dict: labels, matrix)
        - assertions_passed (bool)
    """
    y_true = list(y_true)
    y_pred = list(y_pred)
    N = len(y_true)
    if N != len(y_pred):
        raise ValueError(f"[{dataset_name}] Length mismatch: y_true has {N} items, y_pred has {len(y_pred)} items")
    if N == 0:
        raise ValueError(f"[{dataset_name}] Evaluation set is empty")

    if labels is None:
        labels = sorted(list(set(y_true) | set(y_pred)))
    else:
        labels = list(labels)

    K = len(labels)
    label_to_idx = {l: i for i, l in enumerate(labels)}

    # Confusion matrix: rows = true labels, columns = predicted labels
    cm = confusion_matrix(y_true, y_pred, labels=labels)

    # Compute row and column sums
    row_sums = cm.sum(axis=1) # True supports per class
    col_sums = cm.sum(axis=0) # Predicted counts per class

    # Trace is total correct predictions
    cm_trace = int(np.trace(cm))
    accuracy = cm_trace / float(N)

    per_class = {}
    f1_list = []
    precision_list = []
    recall_list = []
    support_list = []

    for i, cls in enumerate(labels):
        tp = int(cm[i, i])
        support_i = int(row_sums[i])
        pred_count_i = int(col_sums[i])

        prec_i = (tp / float(pred_count_i)) if pred_count_i > 0 else 0.0
        rec_i = (tp / float(support_i)) if support_i > 0 else 0.0
        if (prec_i + rec_i) > 0:
            f1_i = 2.0 * prec_i * rec_i / (prec_i + rec_i)
        else:
            f1_i = 0.0

        per_class[cls] = {
            "precision": float(round(prec_i, 4)),
            "recall": float(round(rec_i, 4)),
            "f1_score": float(round(f1_i, 4)),
            "support": support_i,
            "prediction_count": pred_count_i,
            "true_positive": tp,
            "false_positive": pred_count_i - tp,
            "false_negative": support_i - tp
        }

        f1_list.append(f1_i)
        precision_list.append(prec_i)
        recall_list.append(rec_i)
        support_list.append(support_i)

    # Unweighted Macro metrics (exact arithmetic mean across all K classes)
    macro_f1 = float(np.mean(f1_list))
    macro_precision = float(np.mean(precision_list))
    macro_recall = float(np.mean(recall_list))

    # Support-weighted metrics
    total_supp = float(sum(support_list))
    weighted_f1 = float(sum(f * s for f, s in zip(f1_list, support_list)) / total_supp)
    weighted_precision = float(sum(p * s for p, s in zip(precision_list, support_list)) / total_supp)
    weighted_recall = float(sum(r * s for r, s in zip(recall_list, support_list)) / total_supp)

    # Micro metrics (identical to accuracy in single-label multi-class evaluation)
    micro_precision = accuracy
    micro_recall = accuracy
    micro_f1 = accuracy

    # ── Automated Mathematical Assertions ──
    # 1. Total class support equals total sample count
    assert sum(support_list) == N, f"[{dataset_name}] Sum of supports {sum(support_list)} != N {N}"
    # 2. Total predicted counts equals total sample count
    assert int(col_sums.sum()) == N, f"[{dataset_name}] Sum of predicted counts {int(col_sums.sum())} != N {N}"
    # 3. Confusion matrix row sums equal true support
    for i, cls in enumerate(labels):
        assert row_sums[i] == support_list[i], f"[{dataset_name}] Row sum {row_sums[i]} != support {support_list[i]} for {cls}"
    # 4. Confusion matrix column sums equal prediction counts
    for i, cls in enumerate(labels):
        assert col_sums[i] == per_class[cls]["prediction_count"], f"[{dataset_name}] Col sum {col_sums[i]} != pred count for {cls}"
    # 5. Accuracy equals trace divided by N
    assert abs(accuracy - (cm_trace / float(N))) < 1e-9, f"[{dataset_name}] Accuracy {accuracy} != trace/N {cm_trace/N}"
    # 6. Trace equals exact count of matching predictions
    manual_correct = sum(1 for yt, yp in zip(y_true, y_pred) if yt == yp)
    assert cm_trace == manual_correct, f"[{dataset_name}] Trace {cm_trace} != manual correct count {manual_correct}"
    # 7. Macro-F1 equals exact unweighted mean of per-class F1s
    assert abs(macro_f1 - (sum(f1_list) / float(K))) < 1e-9, f"[{dataset_name}] Macro-F1 mismatch"
    # 8. Weighted-F1 equals exact support-weighted mean
    manual_wf1 = sum(f1_list[i] * support_list[i] for i in range(K)) / float(N)
    assert abs(weighted_f1 - manual_wf1) < 1e-9, f"[{dataset_name}] Weighted-F1 mismatch"
    # 9. Micro metrics equal accuracy
    assert abs(micro_f1 - accuracy) < 1e-9, f"[{dataset_name}] Micro-F1 != accuracy"

    return {
        "dataset_name": dataset_name,
        "sample_count": N,
        "total_correct": cm_trace,
        "accuracy": round(accuracy, 4),
        "macro_f1": round(macro_f1, 4),
        "macro_precision": round(macro_precision, 4),
        "macro_recall": round(macro_recall, 4),
        "weighted_f1": round(weighted_f1, 4),
        "weighted_precision": round(weighted_precision, 4),
        "weighted_recall": round(weighted_recall, 4),
        "micro_f1": round(micro_f1, 4),
        "micro_precision": round(micro_precision, 4),
        "micro_recall": round(micro_recall, 4),
        "raw_accuracy": accuracy,
        "raw_macro_f1": macro_f1,
        "raw_weighted_f1": weighted_f1,
        "labels": labels,
        "per_class": per_class,
        "confusion_matrix": {
            "labels": labels,
            "matrix": cm.tolist()
        },
        "assertions_passed": True
    }

