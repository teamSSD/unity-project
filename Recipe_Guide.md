# Recipe Guide (imageName based)

`imageName`을 기준으로 원재료부터 최종 요리까지의 전체 공정을 정리한 가이드입니다.

---

## 🍱 MAIN 메뉴 (6종)

### 1. 새벽국 (item_dawnSoup_raw)
| 구분 | 단계별 파일명 (`imageName`) | 조합 재료 (`imageName` 리스트) |
| :--- | :--- | :--- |
| **1단계** | item_seaweedBroth_raw | item_pickledSeaweed_raw, item_insulatingMushroom_raw, item_water_raw |
| **2단계** | item_dawnSoupBase_raw | item_seaweedBroth_raw, item_dawnHerb_raw |
| **최종** | **item_dawnSoup_raw** | item_dawnSoupBase_raw, item_tofu_raw |

### 2. 구룡면 (item_guryongNoodles_raw)
| 구분 | 단계별 파일명 (`imageName`) | 조합 재료 (`imageName` 리스트) |
| :--- | :--- | :--- |
| **1단계** | item_boiledWheatNoodles_raw | item_wheatNoodles_raw |
| **1단계** | item_choppedInsulatingMushroom_raw | item_insulatingMushroom_raw |
| **2단계** | item_darkSoy_raw | item_choppedInsulatingMushroom_raw, item_squidInk_raw |
| **2단계** | item_shrimpOilBase_raw | item_azureOil_raw, item_garlic_raw, item_driedShrimp_raw |
| **최종** | **item_guryongNoodles_raw** | item_boiledWheatNoodles_raw, item_darkSoy_raw, item_shrimpOilBase_raw |

### 3. 기계장 고기정식 (item_machineRoomMeatSet_raw)
| 구분 | 단계별 파일명 (`imageName`) | 조합 재료 (`imageName` 리스트) |
| :--- | :--- | :--- |
| **1단계** | item_dicedArtificialMeat_raw | item_artificialMeat_raw |
| **1단계** | item_grilledSteelRoot_raw | item_steelRoot_raw |
| **1단계** | item_jangSauce_raw | item_gochujang_raw, item_blackDoenjang_raw |
| **2단계** | item_grilledArtificialMeat_raw | item_dicedArtificialMeat_raw |
| **최종** | **item_machineRoomMeatSet_raw** | item_grilledArtificialMeat_raw, item_grilledSteelRoot_raw, item_jangSauce_raw |

### 4. 스트리트 스테이크 49 (item_streetSteak49_raw)
| 구분 | 단계별 파일명 (`imageName`) | 조합 재료 (`imageName` 리스트) |
| :--- | :--- | :--- |
| **1단계** | item_artificialMeatSteak_raw | item_smokeLeafButter_raw, item_artificialMeat_raw, item_brassSalt_raw |
| **1단계** | item_caramelTopping_raw | item_moonRoot_raw, item_onion_raw |
| **최종** | **item_streetSteak49_raw** | item_artificialMeatSteak_raw, item_caramelTopping_raw |

### 5. 폐건물 삼각밥 (item_abandonedBuildingRiceBall_raw)
| 구분 | 단계별 파일명 (`imageName`) | 조합 재료 (`imageName` 리스트) |
| :--- | :--- | :--- |
| **1단계** | item_blackRiceBowl_raw | item_blackRice_raw, item_water_raw |
| **2단계** | item_triangleRice_raw | item_blackRiceBowl_raw, item_driedAnchovy_raw, item_blackSesame_raw, item_eggplant_raw, item_cheongyangLeaf_raw |
| **3단계** | item_soyCoatedRice_raw | item_ashenSoySauce_raw, item_triangleRice_raw |
| **최종** | **item_abandonedBuildingRiceBall_raw** | item_newspaper_raw, item_soyCoatedRice_raw |

### 6. 옥상 오믈렛 (item_rooftopOmelette_raw)
| 구분 | 단계별 파일명 (`imageName`) | 조합 재료 (`imageName` 리스트) |
| :--- | :--- | :--- |
| **1단계** | item_omeletteBase_raw | item_lumiEgg_raw |
| **최종** | **item_rooftopOmelette_raw** | item_omeletteBase_raw, item_lumiLeaf_raw, item_lightTomato_raw |

---

## 🥗 SIDE 메뉴 (4종)

### 7. 루미 젤리 (item_lumiJelly_raw)
| 구분 | 단계별 파일명 (`imageName`) | 조합 재료 (`imageName` 리스트) |
| :--- | :--- | :--- |
| **1단계** | item_lumiGelBase_raw | item_lumiEgg_raw |
| **최종** | **item_lumiJelly_raw** | item_lumiGelBase_raw, item_luminousSyrup_raw, item_lemon_raw |

### 8. 네온 샐러드 (item_neonSalad_raw)
| 구분 | 단계별 파일명 (`imageName`) | 조합 재료 (`imageName` 리스트) |
| :--- | :--- | :--- |
| **1단계** | item_grilledWindBean_raw | item_windBean_raw |
| **2단계** | item_saladBase_raw | item_lumiLeaf_raw, item_cheongyangLeaf_raw, item_lightTomato_raw, item_grilledWindBean_raw, item_electricFlower_raw |
| **최종** | **item_neonSalad_raw** | item_saladBase_raw, item_lemon_raw |

### 9. 전력실 꼬치 (item_powerRoomSkewer_raw)
| 구분 | 단계별 파일명 (`imageName`) | 조합 재료 (`imageName` 리스트) |
| :--- | :--- | :--- |
| **1단계** | item_skewerSet_raw | item_syntheticChicken_raw, item_insulatingMushroom_raw, item_greenOnion_raw |
| **최종** | **item_powerRoomSkewer_raw** | item_skewerSet_raw, item_brassSalt_raw |

### 10. 환기구 연어구이 (item_ventGrilledSalmon_raw)
| 구분 | 단계별 파일명 (`imageName`) | 조합 재료 (`imageName` 리스트) |
| :--- | :--- | :--- |
| **1단계** | item_seasonedSalmon_raw | item_artificialSalmon_raw, item_smokeLeafButter_raw, item_cheongyangLeaf_raw |
| **최종** | **item_ventGrilledSalmon_raw** | item_seasonedSalmon_raw, item_newspaper_raw |
