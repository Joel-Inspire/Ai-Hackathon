package com.demandbridge.db_image_generator;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.cloud.client.discovery.EnableDiscoveryClient;

@SpringBootApplication
@EnableDiscoveryClient
public class DbImageGeneratorApplication {

	public static void main(String[] args) {
		SpringApplication.run(DbImageGeneratorApplication.class, args);
	}

}
