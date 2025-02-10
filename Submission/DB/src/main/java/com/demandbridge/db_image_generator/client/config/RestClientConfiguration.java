package com.demandbridge.db_image_generator.client.config;

import com.demandbridge.db_image_generator.client.CatalogClient;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.client.RestClient;
import org.springframework.web.client.support.RestClientAdapter;
import org.springframework.web.service.invoker.HttpServiceProxyFactory;

@Configuration
public class RestClientConfiguration {
    @Value("${api.base-domain}")
    private String baseUrl;

    @Bean
    public CatalogClient catalogRestClient() {
        RestClient restClient = this.createRestClient("https://dbe-catalog");
        RestClientAdapter adapter = RestClientAdapter.create(restClient);
        HttpServiceProxyFactory factory = HttpServiceProxyFactory.builderFor(adapter).build();
        return factory.createClient(CatalogClient.class);
    }


    private RestClient createRestClient(String applicationName) {
        return RestClient.builder()
                .baseUrl(applicationName + baseUrl)
                .build();
    }
}
