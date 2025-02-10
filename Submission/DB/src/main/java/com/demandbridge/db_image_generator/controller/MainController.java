package com.demandbridge.db_image_generator.controller;

import com.demandbridge.db_image_generator.service.MainImageService;


import com.feign.dto.ai_image.PromptRequest;
import io.swagger.v3.oas.annotations.Operation;
import lombok.RequiredArgsConstructor;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController()
@RequiredArgsConstructor
public class MainController {
    private final MainImageService mainImageService;

    @GetMapping("/")
    public String health() {
        return "Welcome to the DemandBridge Image Generator!";
    }

    @PostMapping("/image")
    @Operation(
            summary = "Get an AI generated image based on the provided prompt.",
            description = "This will generate a DALLE-3 image url. For a more generic call - do  not include the category field in the request body."
    )
    public ResponseEntity<String> getAiImage(@RequestBody PromptRequest promptRequest) {
        String response = mainImageService.generateThumbnailImage(promptRequest.getCategory(), promptRequest.getTheme(),
                promptRequest.getMainPrompt(),
                promptRequest.getColorHexes(),
                promptRequest.getInlineText());
        if (response == null) {
            return ResponseEntity.notFound().build();
        } else {
            return ResponseEntity.ok().contentType(MediaType.TEXT_PLAIN).body(response);
        }
    }

}
