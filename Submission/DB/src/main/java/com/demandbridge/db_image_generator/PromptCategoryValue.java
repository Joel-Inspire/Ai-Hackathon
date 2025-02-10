package com.demandbridge.db_image_generator;

import com.feign.dto.ai_image.PromptCategory;
import lombok.Getter;

@Getter
public enum PromptCategoryValue {
    CATALOG_THUMBNAIL(PromptCategory.catalogThumb, "A {theme} image for a catalog thumbnail on an e-commerce site. "
            + " Ensure the background is neutral with no words. Considering that the types of items in the catalog can be categorized with  "
            + "these descriptions: [{mainPrompt}].", ImageParameters.Size.SQUARE_1024, ImageParameters.Resolution.STANDARD_DEFINITION),
    CATALOG_BANNER(PromptCategory.catalogBanner, "A {theme} image for a catalog theme image on an e-commerce site that could be suitable for "
            + " the header banner of a page on an e-commerce site. "
            + " Ensure the background is neutral with no words. Considering that the types of items in the catalog can be categorized with "
            + " these descriptions: [{mainPrompt}].", ImageParameters.Size.RECTANGLE_1792x1024, ImageParameters.Resolution.HIGH_DEFINITION);

    private final PromptCategory category;
    private final String promptText;
    private final ImageParameters.Size size;
    private final ImageParameters.Resolution resolution;
    PromptCategoryValue(PromptCategory category, String promptText, ImageParameters.Size size, ImageParameters.Resolution resolution) {
        this.category = category;
        this.promptText = promptText;
        this.size = size;
        this.resolution = resolution;
    }

    public static PromptCategoryValue getByCategory(PromptCategory category) {
        for (PromptCategoryValue value : values()) {
            if (value.getCategory().equals(category)) {
                return value;
            }
        }
        return null;
    }

}
