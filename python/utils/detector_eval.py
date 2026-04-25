import numpy as np


def compute_iou_xyxy(box_a, box_b):
    ax1, ay1, ax2, ay2 = box_a
    bx1, by1, bx2, by2 = box_b

    inter_x1 = max(ax1, bx1)
    inter_y1 = max(ay1, by1)
    inter_x2 = min(ax2, bx2)
    inter_y2 = min(ay2, by2)

    inter_w = max(0.0, inter_x2 - inter_x1)
    inter_h = max(0.0, inter_y2 - inter_y1)
    inter_area = inter_w * inter_h

    area_a = max(0.0, ax2 - ax1) * max(0.0, ay2 - ay1)
    area_b = max(0.0, bx2 - bx1) * max(0.0, by2 - by1)

    union = area_a + area_b - inter_area
    if union <= 0:
        return 0.0
    return inter_area / union


def greedy_match(gt_boxes, pred_boxes, pred_scores, match_iou_threshold=0.5):
    matched_gt = set()
    matched_pred = set()

    if len(gt_boxes) == 0 or len(pred_boxes) == 0:
        return matched_gt, matched_pred, []

    order = np.argsort(-pred_scores)
    matches = []

    for pred_idx in order:
        best_gt_idx = -1
        best_iou = -1.0

        for gt_idx in range(len(gt_boxes)):
            if gt_idx in matched_gt:
                continue

            iou = compute_iou_xyxy(pred_boxes[pred_idx], gt_boxes[gt_idx])
            if iou > best_iou:
                best_iou = iou
                best_gt_idx = gt_idx

        if best_gt_idx >= 0 and best_iou >= match_iou_threshold:
            matched_gt.add(best_gt_idx)
            matched_pred.add(pred_idx)
            matches.append((best_gt_idx, pred_idx, best_iou))

    return matched_gt, matched_pred, matches