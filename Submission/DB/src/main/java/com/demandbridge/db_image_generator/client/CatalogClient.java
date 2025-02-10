package com.demandbridge.db_image_generator.client;

import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.service.annotation.GetExchange;

import java.util.List;

public interface CatalogClient {

    @GetExchange(value = "/api/catalog/{catalogId}/items/top/{maxItemCount}/descriptions")
    List<String> getTopItemDescriptionsForCatalog(@PathVariable Long catalogId, @PathVariable Integer maxItemCount);


}
