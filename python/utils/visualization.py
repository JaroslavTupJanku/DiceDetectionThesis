import matplotlib.patches as patches


def draw_boxes(ax, image, boxes, title="", fontsize=10):
    ax.imshow(image)

    for xmin, ymin, xmax, ymax in boxes:
        rect = patches.Rectangle(
            (xmin, ymin),
            xmax - xmin,
            ymax - ymin,
            linewidth=2,
            edgecolor="red",
            facecolor="none",
        )
        ax.add_patch(rect)

    ax.set_title(title, fontsize=fontsize)
    ax.axis("off")