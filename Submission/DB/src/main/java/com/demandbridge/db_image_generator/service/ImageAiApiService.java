package com.demandbridge.db_image_generator.service;

import com.demandbridge.db_image_generator.ImageParameters;
import lombok.RequiredArgsConstructor;
import org.springframework.ai.image.ImagePrompt;
import org.springframework.ai.image.ImageResponse;
import org.springframework.ai.openai.OpenAiImageModel;
import org.springframework.ai.openai.OpenAiImageOptions;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class ImageAiApiService {

    private final OpenAiImageModel openaiImageModel;

    public ImageResponse getImageForPrompt(String prompt, OpenAiImageOptions openAiImageOptions) {
        return openaiImageModel.call(
                new ImagePrompt(prompt,
                        openAiImageOptions)
        );
    }

    public OpenAiImageOptions getOpenAiImageOptions(ImageParameters.Size size, ImageParameters.Resolution resolution, int results) {
        return OpenAiImageOptions.builder()
                .quality(resolution.getResolution())
                .N(results)
                .height(size.getHeight())
                .width(size.getWidth())
                .build();
    }


    public OpenAiImageOptions getSquareStandardOptions(int results) {
        return getOpenAiImageOptions(ImageParameters.Size.SQUARE_1024, ImageParameters.Resolution.STANDARD_DEFINITION, results);
    }



}
