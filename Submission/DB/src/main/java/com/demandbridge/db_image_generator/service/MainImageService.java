package com.demandbridge.db_image_generator.service;

import com.demandbridge.db_image_generator.PromptCategoryValue;
import com.feign.dto.ai_image.PromptCategory;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.springframework.ai.image.ImageResponse;
import org.springframework.ai.openai.OpenAiImageOptions;
import org.springframework.stereotype.Service;
import org.springframework.util.CollectionUtils;

import java.util.List;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
@Slf4j
public class MainImageService {
    private final ImageAiApiService imageAiApiService;
    private final static String COLOR_PROMPT = " The image should incorporate these color hex values: ";
    private final static String INLINE_TEXT_PROMPT = " Finally the image should be soft enough so the following phrase can be used EXACTLY as it "
            + "is superimposed in the foreground of the image: `{catalogTitle}` ";

    public String generateThumbnailImage(PromptCategory category, String theme, String mainPrompt, List<String> colorHexes, String inlineText) {
        StringBuilder fullPrompt = new StringBuilder();
        if (StringUtils.isEmpty(mainPrompt)) {
            log.error("mainPrompt is required to generate catalog thumbnail image for category: {}", category);
            return null;
        }
        PromptCategoryValue promptCategoryValue = (category != null) ? PromptCategoryValue.getByCategory(category) : null;
        getFullPrompt(promptCategoryValue, fullPrompt, category, theme, mainPrompt, colorHexes, inlineText);
        try {
            log.info("generating catalog image for prompt: {}", fullPrompt);
            OpenAiImageOptions openAiImageOptions = promptCategoryValue!=null ?
                    imageAiApiService.getOpenAiImageOptions(promptCategoryValue.getSize(),
                            promptCategoryValue.getResolution(), 1) : imageAiApiService.getSquareStandardOptions(1);
            ImageResponse imageResponse = imageAiApiService.getImageForPrompt(fullPrompt.toString(), openAiImageOptions);
            return imageResponse.getResult().getOutput().getUrl();
        }   catch (Exception e) {
            log.error("error generating catalog thumbnail image for prompt: {} {}", fullPrompt, e.getMessage());
            return null;
        }
    }

    private void getFullPrompt(PromptCategoryValue promptCategoryValue, StringBuilder fullPrompt, PromptCategory category, String theme,
                               String mainPrompt, List<String> colorHexes
            , String inlineText) {
        if (promptCategoryValue != null) {
            fullPrompt.append(getFullPromptWithReplacement(category, promptCategoryValue.getPromptText(), theme, mainPrompt, inlineText));
        } else {
            fullPrompt.append(mainPrompt);
        }
        fullPrompt.append(getColorPrompt(colorHexes));
    }

    private String getFullPromptWithReplacement(PromptCategory category, String promptCategoryValue, String theme, String mainPrompt,
                                                String inlineText) {
        if (category == PromptCategoryValue.CATALOG_THUMBNAIL.getCategory()) {
            return promptCategoryValue.replace("{theme}", theme)
                    .replace("{mainPrompt}", mainPrompt);
        } else if (category == PromptCategoryValue.CATALOG_BANNER.getCategory()) {
            return promptCategoryValue.replace("{theme}", theme)
                    .replace("{mainPrompt}", mainPrompt)
                    + (StringUtils.isNotEmpty(inlineText) ? INLINE_TEXT_PROMPT.replace("{catalogTitle}", inlineText) : "");
        }  else {
            return promptCategoryValue.replace("{mainPrompt}", mainPrompt);
        }
    }

    private String getColorPrompt(List<String> colorHexes) {
        String colorPrompt = "";
        if (! CollectionUtils.isEmpty(colorHexes)) {
            colorPrompt = COLOR_PROMPT +
                    colorHexes.stream()
                            .map(colorHex -> "\"" + colorHex + "\"") // Wrap each item with quotes
                            .collect(Collectors.joining(", "))
                    + ".";
        }
        return colorPrompt;
    }


}
