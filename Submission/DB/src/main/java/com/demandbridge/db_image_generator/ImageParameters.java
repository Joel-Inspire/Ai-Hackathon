package com.demandbridge.db_image_generator;

import lombok.Getter;

public class ImageParameters {
    @Getter
    public enum Size {
        SQUARE_1024(1024, 1024),
        RECTANGLE_1024x1792(1024, 1792),
        RECTANGLE_1792x1024(1792, 1024);

        private final int width;
        private final int height;

        Size(int width, int height) {
            this.width = width;
            this.height = height;
        }

    }
    //
    @Getter
    public enum Resolution {
        HIGH_DEFINITION("hd"),
        STANDARD_DEFINITION("standard");
        private final String resolution;

        Resolution(String resolution) {
            this.resolution = resolution;
        }

    }

}

